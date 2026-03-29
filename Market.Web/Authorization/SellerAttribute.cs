using System;
using Microsoft.AspNetCore.Mvc;

namespace Market.Web.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SellerAttribute : TypeFilterAttribute
{
    public SellerAttribute() : base(typeof(SellerFilter))
    {
    }
}
