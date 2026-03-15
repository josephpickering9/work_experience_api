using System.Text.Json.Serialization;

namespace Work_Experience_Search.Services.VertexAi;

public class GoogleSearchResponse
{
    [JsonPropertyName("candidates")]
    public List<Candidate> Candidates { get; set; } = [];
}

public class Candidate
{
    [JsonPropertyName("content")]
    public Content? Content { get; set; }

    [JsonPropertyName("groundingMetadata")]
    public GroundingMetadata? GroundingMetadata { get; set; }
    
    [JsonPropertyName("citationMetadata")]
    public CitationMetadata? CitationMetadata { get; set; }

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }
}

public class Content
{
    [JsonPropertyName("parts")]
    public List<Part> Parts { get; set; } = [];

    [JsonPropertyName("role")]
    public string? Role { get; set; }
}

public class Part
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class GroundingMetadata
{
    [JsonPropertyName("groundingChunks")]
    public List<GroundingChunk> GroundingChunks { get; set; } = [];
    
    [JsonPropertyName("groundingSupports")]
    public List<GroundingSupport> GroundingSupports { get; set; } = [];
}

public class GroundingChunk
{
    [JsonPropertyName("retrievedContext")]
    public RetrievedContext? RetrievedContext { get; set; }
}

public class RetrievedContext
{
    [JsonPropertyName("documentName")]
    public string? DocumentName { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class GroundingSupport
{
    [JsonPropertyName("segment")]
    public SupportSegment? Segment { get; set; }
    
    [JsonPropertyName("groundingChunkIndices")]
    public List<int> GroundingChunkIndices { get; set; } = [];
}

public class SupportSegment
{
    [JsonPropertyName("startIndex")]
    public int? StartIndex { get; set; }
    
    [JsonPropertyName("endIndex")]
    public int? EndIndex { get; set; }
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class CitationMetadata
{
    [JsonPropertyName("citations")]
    public List<Citation> Citations { get; set; } = [];
}

public class Citation
{
    [JsonPropertyName("startIndex")]
    public int? StartIndex { get; set; }

    [JsonPropertyName("endIndex")]
    public int? EndIndex { get; set; }

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }
    
    [JsonPropertyName("source")]
    public string? Source { get; set; } // Sometimes used instead of URI
}
