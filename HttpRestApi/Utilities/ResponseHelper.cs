using HttpRestApi.Models;
using Newtonsoft.Json;

namespace HttpRestApi.Utilities
{
    public class ResponseHelper
    {
        private ResponseModel model;

        public ResponseHelper(bool Status, string Message)
        {
            model.Status = Status;
            model.Message = Message;
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(model);
        }
    }
}
