using System.ComponentModel.DataAnnotations;

namespace Platform.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Поле Логін є обов'язковим")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Поле Пароль є обов'язковим")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}