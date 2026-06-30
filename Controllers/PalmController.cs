using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace PalmScannerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PalmController : ControllerBase
    {
        [HttpGet("init")]
        public IActionResult Init()
        {
            var result = PalmScanner.Init();
            return Ok(new { message = result });  // ✅ JSON wrapped
        }

        [HttpGet("open")]
        public IActionResult OpenDevice()
        {
            var result = PalmScanner.OpenDevice();
            return Ok(new { message = result });  // ✅ JSON wrapped
        }

        [HttpGet("close")]
        public IActionResult CloseDevice()
        {
            var result = PalmScanner.CloseDevice();
            return Ok(new { message = result });  // ✅ JSON wrapped
        }

        [HttpGet("test-odoo")]
        public async Task<IActionResult> TestOdooConnection()
        {
            var result = await PalmScanner.TestOdooConnection();
            return Ok(new { message = result });  // ✅ JSON wrapped
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromQuery] string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return BadRequest(new { error = "Mobile number is required." });

            var result = await PalmScanner.Register(mobile);
            return Ok(new { message = result });  // ✅ JSON wrapped
        }

        [HttpGet("match")]
        public async Task<IActionResult> Match()
        {
            var result = await PalmScanner.Match();
            return Ok(new { message = result });  // ✅ JSON wrapped
        }

        [HttpGet("verify-mobile")]
        public async Task<IActionResult> VerifyMobileTemplate([FromQuery] string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return BadRequest(new { error = "Mobile number is required." });

            var result = await PalmScanner.VerifyMobileTemplate(mobile);
            return Ok(new { message = result });  // ✅ JSON wrapped
        }

        [HttpPost("delete-template")]
        public async Task<IActionResult> DeletePalmTemplate([FromQuery] string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return BadRequest("Mobile number is required.");

            var result = await PalmScanner.DeletePalmTemplate(mobile);
            return Ok(new { message = result });
        }
    }
}
