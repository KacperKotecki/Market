using Microsoft.AspNetCore.Mvc;

namespace Market.Web.Authorization;

public class BuyerAttribute : TypeFilterAttribute
{
    public BuyerAttribute() : base(typeof(BuyerFilter))
    {
    }
}
