using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using HttpRestApi.Utilities;

namespace HttpRestApi.Controllers
{
    /// <summary>
    /// This controller is used to upload files via REST Api. It will then save
    /// the retrieved file into a directory and returns an OK message to the client.
    /// 
    /// Routes:
    /// - file/single :: Able to receive a single file
    /// - file/multiple :: Able to receive multiple files
    /// 
    /// Note:
    /// - This controller does not check the size of the file. If the file is too 
    ///   large, an error will be thrown.
    /// - This controller is able to handle ZIP files.
    /// </summary>
    [ApiController]
    [Route("file")]
    public class FileController : ControllerBase
    {
        public string DEFAULT_FILE_PATH = Directory.GetCurrentDirectory() + @"\Retrieved\";

        [HttpPost]
        [Route("single")]
        public async Task<IActionResult> ProcessSingleFile([Required] IFormFile file)
        {
            // Get file information
            FileInfo fileInfo = new FileInfo(file.FileName);
            string fileName = $"{DateTime.Now:dd-MM-yyyy HH.mm.ss} - {fileInfo.Name}";

            try
            {
                // Log Request
                Logging.Info(Request.ToString()!);


                // Headers
                Logging.Info("[Header] User-Agent :: " + Request.Headers.UserAgent);
                try { Logging.Info("[Header] Keep-Alive :: " + Request.Headers.KeepAlive); } catch { }
                Console.WriteLine("ContentType :: " + Request.ContentType);


                // Save into a directory
                string savePath = DEFAULT_FILE_PATH;
                string fileNameWithPath = Path.Combine(savePath, fileName);
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                using (var stream = new FileStream(fileNameWithPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }


                // Return response with modified response headers
                Response.Headers.ContentDisposition = Request.Headers.ContentDisposition;
                Response.ContentType = "text/plain";
                return Ok(new ResponseHelper(true, "File Uploaded Successfully!").Serialize());
            }
            catch (Exception ex)
            {
                Logging.Error("[ProcessSingleFile] Error :: " + ex.ToString());
            }

            Logging.Empty();

            return BadRequest(new ResponseHelper(false, "Something went wrong with the file upload!").Serialize());
        }

        [HttpPost]
        [Route("multiple")]
        public async Task<IActionResult> ProcessMultipleFile([Required] List<IFormFile> files)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Log Request
                    Logging.Info(Request.ToString()!);


                    // Headers
                    Logging.Info("[Header] User-Agent :: " + Request.Headers.UserAgent);
                    try { Logging.Info("[Header] Keep-Alive :: " + Request.Headers.KeepAlive); } catch { }
                    Console.WriteLine("ContentType :: " + Request.ContentType);


                    // Check save path directory
                    string savePath = DEFAULT_FILE_PATH;
                    if (!Directory.Exists(savePath))
                    {
                        Directory.CreateDirectory(savePath);
                    }


                    // Check file count
                    int fileCount = files.Count;


                    // Iterate through each files
                    if (fileCount > 0)
                    {
                        foreach (var file in files)
                        {
                            // Get file information
                            FileInfo fileInfo = new FileInfo(file.FileName);
                            string fileName = $"{DateTime.Now:dd-MM-yyyy HH.mm.ss} - {fileInfo.Name}";

                            string fileNameWithPath = Path.Combine(savePath, fileName);

                            using (var stream = new FileStream(fileNameWithPath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }
                        }
                    }

                    // Return response with modified response headers
                    Response.Headers.ContentDisposition = Request.Headers.ContentDisposition;
                    Response.ContentType = "text/plain";
                    return Ok(new ResponseHelper(true, $"{fileCount} files Uploaded Successfully!").Serialize());
                }
            }
            catch (Exception ex)
            {
                Logging.Error("[ProcessMultipleFile] Error :: " + ex.ToString());
            }

            Logging.Empty();

            return BadRequest(new ResponseHelper(false, "Something went wrong with the file upload!").Serialize());
        }
    }
}
