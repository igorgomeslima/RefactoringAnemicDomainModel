using System;
using System.Collections.Generic;
using System.Linq;
using Logic.Common;
using Logic.Movies;

namespace Logic.Customers
{
    public class Customer : Entity
    {
        private string _name;
        public virtual CustomerName Name
        {
            //get => CustomerName.Create(_name).Value;
            //set => _name = value.Value;
            get => (CustomerName)_name; //Conversao explicita;
            set => _name = value; //Conversao implicita;
        }

        private readonly string _email;
        public virtual Email Email => (Email)_email; //Conversao explicita;

        public virtual CustomerStatus Status { get; protected set; }

        private decimal _moneySpent;
        public virtual Dollars MoneySpent
        {
            //get => Dollars.Create(_moneySpent).Value;
            //set => _moneySpent = value.Value;
            get => Dollars.Of(_moneySpent); //Conversao explicita;
            protected set => _moneySpent = value; //Conversao implicita;
        }

        private readonly IList<PurchasedMovie> _purchasedMovies;
        public virtual IReadOnlyList<PurchasedMovie> PurchasedMovies => _purchasedMovies.ToList();

        protected Customer()
        {
            _purchasedMovies = new List<PurchasedMovie>();
        }

        public Customer(CustomerName name, Email email) : this() //this() Chama o construtor `protect` garantindo que _purchasedMovies seja instanciado.
        {
            _name = name ?? throw new ArgumentException(nameof(name));
            _email = email ?? throw new ArgumentException(nameof(email));

            MoneySpent = Dollars.Of(0);
            Status = CustomerStatus.Regular;
        }

        public virtual bool HasPurchasedMovie(Movie movie)
        {
            return PurchasedMovies.Any(x => x.Movie == movie && !x.ExpirationDate.IsExpired);
        }

        //public virtual void AddPurchasedMovie(PurchasedMovie purchasedMovie, Dollars price)
        public virtual void PurchasedMovie(Movie movie)
        {
            if (HasPurchasedMovie(movie))
                throw new Exception();

            ExpirationDate expirationDate = movie.GetExpirationDate();
            Dollars price = movie.CalculatePrice(Status);

            var purchasedMovie = new PurchasedMovie(movie, this, price, expirationDate);
            _purchasedMovies.Add(purchasedMovie);
            MoneySpent += price;
        }

        public virtual Result CanPromote()
        {
            if (Status.IsAdvanced)
                return Result.Fail("The customer already has the Advanced status.");

            if (PurchasedMovies.Count(x => x.ExpirationDate == ExpirationDate.Infinite || x.ExpirationDate.Date >= DateTime.UtcNow.AddDays(-30)) < 2)
                return Result.Fail("The customer has to have at least 2 active movies during the last 30 days.");

            if (PurchasedMovies.Where(x => x.PurchaseDate > DateTime.UtcNow.AddYears(-1)).Sum(x => x.Price) < 100m)
                return Result.Fail("The customer has to have at least 100 dollars spent during the last year.");

            return Result.Ok();
        }

        public virtual void Promote()
        {
            if (CanPromote().IsFailure)
                throw new Exception();

            Status = Status.Promote();
        }
    }
}
