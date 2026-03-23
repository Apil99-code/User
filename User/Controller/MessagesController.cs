using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using User.Hubs;
namespace User.Controllers
{

  [ApiController]
  [Route("api/messages")]
  public class MessagesController(IHubContext<ChatHub> hub) : ControllerBase
  {
    private readonly IHubContext<ChatHub> _hub = hub;

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastMessageRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.Message))
        return BadRequest(new { message = "Message is required" });

      await _hub.Clients.All.SendAsync("ReceiveNotification", new
      {
        username = "System",
        message = request.Message.Trim(),
        timestamp = DateTime.UtcNow
      });

      return Ok(new { message = "Message broadcasted" });
    }

    public class BroadcastMessageRequest
    {
      public string Message { get; set; } = string.Empty;
    }
  }
}
