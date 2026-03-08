using Microsoft.AspNetCore.Mvc;

namespace Market.Web.Authorization;

public class SellerAttribute : TypeFilterAttribute
{
    public SellerAttribute() : base(typeof(SellerFilter))
    {
    }
}
