using Microsoft.Extensions.FileProviders;
using static System.Net.Mime.MediaTypeNames;

namespace ImageUploader
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddAntiforgery();
            var app = builder.Build();

            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UploadedImages")),
                RequestPath = "/UploadedImages"
            });

            app.MapGet("/", async context =>
            {
                context.Response.Redirect("/Index.html");
            });

            var Images = new List<Models.Image>();
            app.MapPost("/upload", async (HttpContext context) =>
            {
                var Form = await context.Request.ReadFormAsync();
                var File = Form.Files.GetFile("image");
                var Title = Form["title"];

                if (File is null || File.Length == 0)
                {
                    return Results.NotFound("No File Uploaded");
                }

                var AllowedExtensions = new[] { ".png", ".jpeg", ".gif" };
                var FileExtension = Path.GetExtension(File.FileName).ToLower();

                if (string.IsNullOrEmpty(FileExtension) || !AllowedExtensions.Contains(FileExtension))
                {
                    return Results.NotFound("Invalid File Extension");
                }

                var ImageId = Guid.NewGuid().ToString();
                var FileName = ImageId + FileExtension;
                var DirectoryPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "UploadedImages");
                var FilePath = Path.Combine(DirectoryPath, FileName);

                using (var stream = new FileStream(FilePath, FileMode.Create))
                {
                    await File.CopyToAsync(stream);
                }

                //Create object from anonymous type
                var UploadedImage = new Models.Image
                {
                    ImageId = ImageId,
                    Title = Title,
                    FileName = FileName,
                };
                Images.Add(UploadedImage);
                return Results.Redirect($"/picture/{ImageId}");
            }
            );

            app.MapGet("/picture/{ImageId}", async (HttpContext context) =>
            {
                var ImageId = context.Request.RouteValues["ImageId"]?.ToString();
                if (string.IsNullOrEmpty(ImageId))
                {
                    return Results.NotFound();
                }

                var Image = Images.FirstOrDefault(Img => Img.ImageId == ImageId);
                if (Image == null)
                {
                    return Results.NotFound();
                }

                context.Response.ContentType = "text/html";
                var TitleContent = $"<h3>{Image.Title}</h3>";
                var ImgContent = $"<img src=\"/UploadedImages/{Image.FileName}\" alt=\"{Image.Title}\"/>";
                await context.Response.WriteAsync(TitleContent);
                await context.Response.WriteAsync(ImgContent);

                return Results.Ok();
            }
            );
            app.Run();
        }
    }
}
