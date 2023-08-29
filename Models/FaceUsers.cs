using System.ComponentModel.DataAnnotations;

namespace FaceRecognition.Models;

public class FaceUser
{
    public string? Id { get; set; }
    
    [Required]
    public string? Nome { get; set; }
    public byte[]? Face { get; set; }
}
