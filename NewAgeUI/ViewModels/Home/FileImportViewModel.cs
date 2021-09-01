using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace NewAgeUI.ViewModels
{
  public class FileImportViewModel
  {
    [Required(ErrorMessage = "Required")]
    public IFormFile CSVFile { get; set; }
    
    [Required(ErrorMessage = "Required")]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }
  }
}
