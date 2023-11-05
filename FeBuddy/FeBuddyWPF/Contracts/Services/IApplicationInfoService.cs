using System;

namespace FeBuddyWPF.Contracts.Services;

public interface IApplicationInfoService
{
    Version GetVersion();
}
