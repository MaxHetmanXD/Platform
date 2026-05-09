using System.ComponentModel.DataAnnotations;

namespace Platform.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Введіть логін")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Введіть пароль")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}