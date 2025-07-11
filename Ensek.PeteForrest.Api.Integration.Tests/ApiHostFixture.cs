using System.Data.Common;
using Ensek.PeteForrest.Db.Creater;
using Ensek.PeteForrest.Infrastructure.Data;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ensek.PeteForrest.Api.Integration.Tests {
    public sealed class ApiHostFixture : IDisposable {
        private TestServer? _server;

        private HttpClient? _client;

        public MeterContext Context { get; }

        public ApiHostFixture() {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            // These options will be used by the context instances in this test suite, including the connection opened above.
            var contextOptions = new DbContextOptionsBuilder<MeterContext>()
                .UseSqlite(_connection)
                .Options;

            // Create the schema and seed some data
            this.Context = new MeterContext(contextOptions);
            this.Context.Database.EnsureCreated();
            AccountSeeder.InsertAccountsFromCsvAsync(this.Context, "Data/Test_Accounts 2.csv").GetAwaiter().GetResult();
        }

        private static readonly SemaphoreSlim Mutex = new(1, 1);

        private DbConnection? _connection;

        public TestServer Server {
            get {
                // try early return if available
                if (this._server != null)
                    return this._server;

                // wait for exclusive access
                Mutex.Wait();

                try {
                    return this._server ??= this.BuildServer();
                }
                finally {
                    Mutex.Release();
                }
            }
        }

        private TestServer BuildServer() {
            var builder = WebHost.CreateDefaultBuilder(null!).UseStartup<Startup>();

            builder.ConfigureAppConfiguration(
                (_, config) => {
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

        public HttpClient Client => this._client ??= this.GetNewClient();

        public HttpClient GetNewClient() {
            return this.Server.CreateClient();
        }

        public void Dispose() {
            if (this._connection != null)
            {
                this._connection.Dispose();
                this._connection = null;
            }

            if (this._server != null)
            {
                this._server.Dispose();
                this._server = null;
            }
        }
    }
}