namespace FEBuddyLibrary.Interfaces;

public interface IappSettings
{
	bool DevMode { get; set; }
	public string OutputDirectory { get; set; }
}