using System.Data.Common;
using Ensek.PeteForrest.Infrastructure.Data;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ensek.PeteForrest.Api.Tests {
    public class ApiHostFixture : IDisposable {
        private TestServer? server;

        private HttpClient? client;

        public MeterContext Context { get; }

        public ApiHostFixture() {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            // These options will be used by the context instances in this test suite, including the connection opened above.
            var _contextOptions = new DbContextOptionsBuilder<MeterContext>()
                .UseSqlite(_connection)
                .Options;

            // Create the schema and seed some data
            this.Context = new MeterContext(_contextOptions);
            this.Context.Database.EnsureCreated();
            DbInitializer.InsertAccountsFromCsv(this.Context, "Data/Test_Accounts 2.csv");
        }


        private static readonly SemaphoreSlim Mutex = new(1, 1);

        private DbConnection? _connection;

        public TestServer Server {
            get {
                // try early return if available
                if (this.server != null)
                    return this.server;

                // wait for exclusive access
                Mutex.Wait();

                try {
                    return this.server ??= this.BuildServer();
                }
                finally {
                    Mutex.Release();
                }
            }
        }

        private TestServer BuildServer() {
            var builder = WebHost.CreateDefaultBuilder(null!).UseStartup<Startup>();

            builder.ConfigureAppConfiguration(
                (context, config) => {
                    config.AddInMemoryCollection()
                          .AddEnvironmentVariables();
                });

            builder.ConfigureTestServices(
                services => {
                    var databaseDescriptor = new ServiceDescriptor(typeof(MeterContext), this.Context);
                    services.Replace(databaseDescriptor);
                });

            return new TestServer(builder);
        }

        public HttpClient Client => this.client ??= this.GetNewClient();

        public HttpClient GetNewClient() {
            return this.Server.CreateClient();
        }

        public void Dispose() {
            if (this._connection != null)
            {
                this._connection.Dispose();
                this._connection = null;
            }

            if (this.server != null)
            {
                this.server.Dispose();
                this.server = null;
            }
        }
    }
}