﻿using System;

namespace Project.ApplicationCore.Entities
{
    public class CodeToken
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string ClientId { get; set; }
        public int UserId { get; set; }
        public DateTime ExpiresIn { get; } = DateTime.UtcNow.AddSeconds(60);
        public string RedirectUri { get; set; }
        public string CodeChallenge { get; set; }
        public string CodeChallengeMethod { get; set; }
    }
}