using System.ComponentModel.DataAnnotations;
using Amazon.S3;
using Amazon.S3.Model;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add AWS Lambda support. When application is run in Lambda Kestrel is swapped out as the web server with Amazon.Lambda.AspNetCoreServer. This
// package will act as the webserver translating request and responses between the Lambda event source and ASP.NET Core.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);
builder.Services.AddAWSService<IAmazonS3>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.UseSwagger().UseSwaggerUI();

app.MapPost("/create-bucket/{name}",
    async (IAmazonS3 amazonS3Client, [Required] string name, CancellationToken token) =>
    {
        try
        {
            await amazonS3Client.PutBucketAsync(new PutBucketRequest { BucketName = name }, token);

            return Results.Ok($"bucket {name} is created.");
        }
        catch (Exception ex)
        {
            return Results.Problem($"Could not create a bucket. {ex.StackTrace}");
        }
    });

app.MapGet("/list-buckets", async (IAmazonS3 amazonS3Client, CancellationToken token) =>
{
    try
    {
        var listBucketsResponse = await amazonS3Client.ListBucketsAsync(new ListBucketsRequest(), token);

        return Results.Ok(listBucketsResponse.Buckets);
    }
    catch (Exception ex)
    {
        return Results.Problem("Could not create a bucket.", ex.StackTrace);
    }
});

app.MapDelete("/delete-bucket/{name}",
    async (IAmazonS3 amazonS3Client, [Required] string name, CancellationToken token) =>
    {
        try
        {
            await amazonS3Client.DeleteBucketAsync(new DeleteBucketRequest { BucketName = name }, token);

            return Results.Ok($"bucket {name} is deleted.");
        }
        catch (Exception ex)
        {
            return Results.Problem("Could not create a bucket.", ex.StackTrace);
        }
    });

app.MapPost("/enable-versioning-bucket/{name}", async (IAmazonS3 amazonS3Client,
    [Required] string name, CancellationToken token) =>
{
    try
    {
        await amazonS3Client.PutBucketVersioningAsync(
            new PutBucketVersioningRequest
            {
                BucketName = name,
                VersioningConfig = new S3BucketVersioningConfig { Status = VersionStatus.Enabled }
            }, token);

        return Results.Ok($"bucket {name} is enabled.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Could not enabled a bucket versioning.", ex.StackTrace);
    }
});

app.MapPost("/create-folder/{bucketName}/{folderName}", async (IAmazonS3 amazonS3Client, [Required] string bucketName,
    [Required] string folderName, CancellationToken token) =>
{
    try
    {
        await amazonS3Client.PutObjectAsync(
            new PutObjectRequest { BucketName = bucketName, Key = folderName.Replace("%2F", "/") }, token);

        return Results.Ok($"bucket {bucketName} folderName {folderName} is created.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Could not creating", ex.StackTrace);
    }
});


app.MapPost("/create-object/{bucketName}/{objectName}", async (IAmazonS3 amazonS3Client, [Required] string bucketName,
    [Required] string objectName, CancellationToken token) =>
{
    try
    {
        await amazonS3Client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectName,
                ContentType = "text/plain",
                ContentBody = "Welcome to Minimal API AWS SDK S3 Development"
            }, token);

        return Results.Ok($"file {bucketName} objectName {objectName} is created.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Could not creating", ex.StackTrace);
    }
});

app.MapPut("/add-metadata-bucket/{bucketName}/{fileName}", async (IAmazonS3 amazonS3Client,
    [Required] string bucketName, [Required] string fileName, CancellationToken token) =>
{
    try
    {
        await amazonS3Client.PutObjectTaggingAsync(
            new PutObjectTaggingRequest
            {
                Tagging = new Tagging
                {
                    TagSet = new List<Tag>
                    {
                        new() { Key = "test-metadata-key", Value = "test-metadata-value" }
                    }
                }
            }, token);

        return Results.Ok($"bucket {bucketName} is adding metadata.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Could not enabled a bucket versioning.", ex.StackTrace);
    }
});


app.MapGet("/generate-download-link/{bucketName}/{keyName}", (IAmazonS3 amazonS3Client, [Required] string bucketName,
    [Required] string keyName, CancellationToken token) =>
{
    try
    {
        var preSignedUrl = amazonS3Client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = bucketName, Key = keyName, Expires = DateTime.Now.AddHours(6), Protocol = Protocol.HTTP
        });

        return Results.Ok(preSignedUrl);
    }
    catch (Exception ex)
    {
        return Results.Problem("Could not enabled a bucket versioning.", ex.StackTrace);
    }
});
app.Run();
