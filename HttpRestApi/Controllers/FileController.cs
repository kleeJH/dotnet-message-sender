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
    /// - file/messagesender :: Used to retrieve files from MessageSender
    /// - file/single :: Able to receive a single file
    /// - file/multiple :: Able to receive multiple files
    /// 
    /// API call for client: (All files are encoded in bytes)
    /// - file/messagesender :: File is written into the request body E.g. RequestBody = {File}
    /// - file/single :: File is written into the request body but requires to be in the "file" field. E.g. RequestBody = {"file": File}
    /// - file/multiple :: Files is written into the request body but requires to be in the "files" field. E.g. RequestBody = {"files": [File1, File2, ...]}
    /// 
    /// Note:
    /// - This controller does not check the size of the file. If the file is too 
    ///   large, an error will be thrown.
    /// - This controller is able to handle ZIP files.
    /// - Accepted ContentType is "multipart/form-data" for file/single and file/multiple
    /// - CORS might be a problem
    /// </summary>
    [ApiController]
    [Route("file")]
    public class FileController : ControllerBase
    {
        public string DEFAULT_FILE_PATH = Directory.GetCurrentDirectory() + @"\Retrieved\";

        [HttpPost]
        [Route("messagesender")]
        [Consumes("text/plain", "application/json", "application/xml", "application/soap+xml", "application/zip")]
        public async Task<IActionResult> ProcessMessageSender()
        {
            // Problem: It does not know what file this is. That's why the ContentType is used to differenciate what file it receive

            string fileName = $"{DateTime.Now:dd-MM-yyyy HH.mm.ss}";

            try
            {
                // Get Body from Request
                Stream file = Request.Body;


                // Log Request
                Logging.Info(Request.ToString()!);


                // Headers
                Logging.Info("[Header] User-Agent :: " + Request.Headers.UserAgent);
                try { Logging.Info("[Header] Keep-Alive :: " + Request.Headers.KeepAlive); } catch { }
                Console.WriteLine("ContentType :: " + Request.ContentType);


                // Identify File Type and append extensions
                switch (Request.ContentType)
                {
                    case "text/plain":
                    case "application/xml":
                    case "application/json":
                    case "application/soap+xml":
                        fileName += ".txt";
                        break;
                    case "application/zip":
                        fileName += ".zip";
                        break;
                }


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
                Logging.Error("[ProcessMessageSender] Error :: " + ex.ToString());
            }

            Logging.Empty();

            return BadRequest(new ResponseHelper(false, "Something went wrong with the file upload!").Serialize());
        }

        [HttpPost]
        [Route("single")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ProcessSingleFile([FromBody] IFormFile file)
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
        [Consumes("multipart/form-data")]
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
