using Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// This class is used to allow controller to accept "text/plain" content.
/// If success, it will return whatever you defined in the controller. Check MessageController -> "text/plain" condition for the return.
/// If it fails, it will end in a failure.
/// 
///  Reference: https://makolyte.com/aspnetcore-how-to-receive-a-text-plain-request/
/// </summary>
public class TextSingleValueFormatter : InputFormatter
{
    private const string TextPlain = "text/plain";
    public TextSingleValueFormatter()
    {
        SupportedMediaTypes.Add(TextPlain);
    }
    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
    {
        try
        {
            using (var reader = new StreamReader(context.HttpContext.Request.Body))
            {
                string textSingleValue = await reader.ReadToEndAsync();
                //Convert from string to target model type (this is the parameter type in the action method)
                object model = Convert.ChangeType(textSingleValue, context.ModelType);
                return InputFormatterResult.Success(model);
            }
        }
        catch (Exception ex)
        {
            context.ModelState.TryAddModelError("BodyTextValue", $"{ex.Message} ModelType={context.ModelType}");
            return InputFormatterResult.Failure();
        }
    }

    protected override bool CanReadType(Type type)
    {
        //Put whatever types you want to handle. 
        return type == typeof(string) ||
            type == typeof(int) ||
            type == typeof(DateTime);
    }
    public override bool CanRead(InputFormatterContext context)
    {
        return context.HttpContext.Request.ContentType == TextPlain;
    }
}