﻿using NodaTime;

namespace Project.WebMVC.ViewModels
{
    public class ProfileViewModel
    {
        public string Username { get; set; }
        public string ProfileImageUrl { get; set; }
        public LocalDate Birthday { get; set; }
    }
}