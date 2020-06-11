using System.ComponentModel.DataAnnotations;

namespace NewAgeUI.Utilities
{
  public class ValidEmailDomainAttribute : ValidationAttribute
  {
    private readonly string _allowedDomain;

    public ValidEmailDomainAttribute(string allowedDomain)
    {
      _allowedDomain = allowedDomain;
    }

    public override bool IsValid(object value)
    {
      string enteredDomain = value.ToString().Split('@')[1];
      return enteredDomain.ToLower() == _allowedDomain.ToLower();
    }
  }
}
