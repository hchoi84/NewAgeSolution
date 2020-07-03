using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FileReaderLibrary
{
  public interface IFileReader
  {
    Task<List<string>> GetSkusFromFileAsync(IFormFile file);
    StringBuilder ConvertToStoreBufferStringBuilder(Dictionary<string, int> skusAndStoreQty);
  }
}
