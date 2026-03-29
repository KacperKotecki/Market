using System;
using Microsoft.AspNetCore.Mvc;

namespace Market.Web.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class BuyerAttribute : TypeFilterAttribute
{
    public BuyerAttribute() : base(typeof(BuyerFilter))
    {
    }
}
