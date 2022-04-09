﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sentaku.ApplicationCore.Aggregates.VoteSessionAggregate;
using Sentaku.ApplicationCore.Aggregates.VotingManagerAggregate;

namespace Sentaku.Infrastructure.Data.Config;

public class VoteSessionConfig: IEntityTypeConfiguration<VoteSession>
{
  public void Configure(EntityTypeBuilder<VoteSession> builder)
  {
    builder
      .HasOne(_ => _.VotingManager)
      .WithMany()
      .HasForeignKey(_ => _.VotingManagerId)
      .OnDelete(DeleteBehavior.SetNull);

    builder
      .Property(_ => _.Agenda)
      .HasMaxLength(2000)
      .IsUnicode()
      .IsRequired();

    builder
      .Property(_ => _.State)
      .IsRequired();
    
    builder
      .Property(_ => _.CreatedOn)
      .IsRequired();
    
    builder
      .Property(_ => _.StartDate)
      .IsRequired();
  }
}
