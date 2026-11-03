namespace IChat.Api.IntegrationTests;

using IChat.Api.Endpoints.Documents.V1.DTOs;
using IChat.Api.Endpoints.Documents.V1.Validators;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

public class UploadDocumentValidationTests
{
    private const long FiftyMegabytes = 50 * 1024 * 1024;

private static UploadDocumentDto DtoOfSize(long sizeInBytes) => new()
    {
        File = new FormFile(Stream.Null, 0, sizeInBytes, "file", "handbook.pdf")
    };

[Fact]
    public void Validate_AcceptsFileExactlyAtTheLimit()
    {
        var result = new UploadDocumentDtoValidator().Validate(DtoOfSize(FiftyMegabytes));

result.IsValid.Should().BeTrue("50MB chẵn vẫn nằm trong trần cho phép");
    }

[Fact]
    public void Validate_RejectsFileOneByteOverTheLimit()
    {
        var result = new UploadDocumentDtoValidator().Validate(DtoOfSize(FiftyMegabytes + 1));

result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("50MB");
    }
}
