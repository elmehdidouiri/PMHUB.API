 using System;

namespace PMHUB.Domain.ValueObjects
{
    public class Money : IEquatable<Money>
    {
        public decimal Amount { get; }
        public string Currency { get; }

        private Money(decimal amount, string currency)
        {
            if (amount < 0)
                throw new ArgumentException("Le montant ne peut pas être négatif", nameof(amount));

            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("La devise est requise", nameof(currency));

            Amount = amount;
            Currency = currency.ToUpper();
        }

        public static Money Create(decimal amount, string currency = "EUR")
        {
            return new Money(amount, currency);
        }

        public static Money Zero(string currency = "EUR")
        {
            return new Money(0, currency);
        }

        public Money Add(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException(
                    $"Impossible d'additionner {Currency} et {other.Currency}");

            return new Money(Amount + other.Amount, Currency);
        }

        public Money Multiply(decimal factor)
        {
            return new Money(Amount * factor, Currency);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Money);
        }

        public bool Equals(Money? other)
        {
            return other != null &&
                   Amount == other.Amount &&
                   Currency == other.Currency;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Amount, Currency);
        }

        public override string ToString()
        {
            return $"{Amount:F2} {Currency}";
        }

        public static bool operator ==(Money? left, Money? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Money? left, Money? right)
        {
            return !Equals(left, right);
        }

        public static Money operator +(Money left, Money right)
        {
            return left.Add(right);
        }

        public static Money operator *(Money money, decimal factor)
        {
            return money.Multiply(factor);
        }
    }
}