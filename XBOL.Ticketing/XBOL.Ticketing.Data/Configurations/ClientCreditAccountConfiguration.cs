using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Configurations
{
    public class ClientCreditAccountConfiguration : IEntityTypeConfiguration<ClientCreditAccount>
    {
        public void Configure(EntityTypeBuilder<ClientCreditAccount> builder)
        {
            builder.Property(x => x.ClientId).IsRequired();

            builder.Property(x => x.CreditLimit)
                .HasColumnType("decimal(19,4)");

            builder.HasMany(x => x.ClientCreditTransactions)
                .WithOne(x => x.ClientCreditAccount);

            builder.HasOne(x => x.Client)
                .WithOne(x => x.ClientCreditAccount);
        }
    }
}
