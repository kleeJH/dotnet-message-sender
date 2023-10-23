namespace HttpRestApi.Models
{
    public struct ResponseModel
    {
        public bool Status { get; set; }
        public object Data { get; set; }
        public string Message { get; set; }
    }
}
