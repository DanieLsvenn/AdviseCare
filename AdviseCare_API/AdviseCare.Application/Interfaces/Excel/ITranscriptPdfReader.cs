using AdviseCare.Application.DTOs;

namespace AdviseCare.Application.Interfaces.Excel
{
    // Reads a transcript PDF (grade or attendance) into its header fields and subject rows. Never throws for a bad file:
    // problems with the file come back in ParsedTranscript.Errors.
    public interface ITranscriptPdfReader
    {
        ParsedTranscript Read(byte[] content);
    }
}
