using Microsoft.AspNetCore.Mvc;
using MQMailSender.Models;
using MQMailSender.Services;

namespace MQMailSender.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SendEMailController : ControllerBase
    {
        private readonly EmailService _emailService;

        public SendEMailController(EmailService emailService)
        {
            _emailService = emailService;
        }
        [HttpPost(Name ="SendEmail")]
        public IActionResult Post([FromBody] EmailDto emailDto)
        {
            _emailService.SendEmail(emailDto);
            return Ok("ÓÊ¼þÒÑ·¢ËÍ");
        }
    }
}
