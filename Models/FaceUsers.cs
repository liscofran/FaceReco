using System.ComponentModel.DataAnnotations;

namespace FaceRecognition.Models;

public class FaceUser
{
    public int Id { get; set; }

    [Required]
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? Face { get; set; }
}