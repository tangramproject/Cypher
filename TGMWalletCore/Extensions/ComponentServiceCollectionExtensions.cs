using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using TGMWalletCore.Actor;
using TGMWalletCore.Coin;
using TGMWalletCore.Send;
using TGMWalletCore.Wallet;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WalletCoreCollection
    {
        public static IServiceCollection AddWalletCore(this IServiceCollection services)
        {
            services.AddSingleton<IActorService, ActorService>();
            services.AddSingleton<ISendService, SendService>();
            services.AddSingleton<IWalletService, WalletService>();
            services.AddSingleton<IBuilderService, BuilderService>();
            return services;
        }
    }
}
