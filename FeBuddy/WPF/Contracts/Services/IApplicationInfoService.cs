using System;

namespace WPF.Contracts.Services;

public interface IApplicationInfoService
{
    Version GetVersion();
}
