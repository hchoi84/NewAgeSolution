namespace EmailSenderLibrary.Utilities
{
  public enum EmailSenderTypeEnum
  {
    EmailConfirmation,
    PasswordReset
  }

  public enum EmailSenderServerEnum
  {
    Gmail,
    Rackspace
  }

  public enum EmailSenderConfirmationEnum
  {
    Success,
    Failed
  }
}
