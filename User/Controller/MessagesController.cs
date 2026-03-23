using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using User.Hubs;
namespace User.Controllers
{

    [ApiController]
    [Route("api/messages")]
    public class MessagesController(IHubContext<ChatHub> hub) : ControllerBase
    {

      //  [HttpPost]
        //public async Task<IActionResult> PostMessage()
        //{
           
        //}
    }
}
