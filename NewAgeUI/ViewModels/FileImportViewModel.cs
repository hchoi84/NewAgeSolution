using Microsoft.AspNetCore.Http;

namespace NewAgeUI.ViewModels
{
  public class FileImportViewModel
  {
    // TODO: implement validation
    // ensure file type is csv
    public IFormFile CSVFile { get; set; }
  }
}
