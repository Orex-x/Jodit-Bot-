using System.Threading.Tasks;
using Jodit_Bot_.Services;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace Jodit_Bot_.Controllers
{
    public class WebhookController: ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromServices] HandleUpdateService handleUpdateService,
            [FromBody] Update update)
        {
            await handleUpdateService.EchoAsync(update);
            return Ok();
        }
    }
}