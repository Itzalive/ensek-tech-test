using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Ensek.PeteForrest.Api.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class AccountController(IAccountRepository accountRepository) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<Account[]>> ListAsync()
        {
            return this.Ok(await accountRepository.GetAsync());
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<ActionResult<Account>> GetAsync(int id)
        {
            var account = await accountRepository.GetAsync(id);
            if (account == null)
            {
                return this.NotFound();
            }

            return this.Ok(account);
        }
    }
}
