using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ECSDiscord.Services.Minecraft;

[ApiController]
[Route("api/minecraft")]
public class MinecraftApiController : ControllerBase
{
    private readonly MinecraftService _minecraftService;
    private readonly MinecraftAccountUpdateSource _accountUpdateSource;

    public MinecraftApiController(MinecraftService minecraftService, MinecraftAccountUpdateSource accountUpdateSource)
    {
        _minecraftService = minecraftService ?? throw new ArgumentNullException(nameof(minecraftService));
        _accountUpdateSource = accountUpdateSource ?? throw new ArgumentNullException(nameof(accountUpdateSource));
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts(bool wait = false, int timeout = 1000)
    {
        if (wait)
        {
            try
            {
                var timeoutSpan = TimeSpan.FromMilliseconds(timeout);
                if (timeoutSpan > TimeSpan.FromMinutes(5))
                {
                    return BadRequest("Timeout value must be less than 5 minutes.");
                }
                var cts = new CancellationTokenSource(timeoutSpan);
                await _accountUpdateSource.WaitForUpdateAsync(cts.Token);
            }
            catch (OperationCanceledException e)
            {
                // Do nothing if timed out.
            }
        }
        var accounts = await _minecraftService.GetAllMinecraftAccountsAsync();
        return Ok(accounts.Select(x => new { uuid = x.MinecraftUuid }));
    }
}