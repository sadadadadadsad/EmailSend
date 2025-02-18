using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace MQMailSender.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IWebHostEnvironment webHostEnvironment;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        public HomeController(IWebHostEnvironment webHostEnvironment)
        {
            this.webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// 获取文件路径
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public string Get()
        {
            string filePath = Path.Combine(webHostEnvironment.ContentRootPath, "appsettings.json");
            return filePath;
        }
    }
}
