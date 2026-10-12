using System;

namespace FeBuddy.Installer.UI.Setup;

/// <summary>
/// Turns Windows Installer's progress messages into one 0-1 fraction for the progress bar, following
/// "Parsing Windows Installer Messages" in the Windows Installer documentation.
/// </summary>
/// <remarks>
/// Windows Installer reports progress in "ticks" against a total it resets for each phase: first
/// while it writes the install script, then while it runs it (and backwards, during a rollback).
/// The script phase is quick, so it fills the first <see cref="ScriptShare"/> of the bar and the
/// run fills the rest. The fraction only moves forward, except during a rollback, when it runs back
/// down so the bar shows the changes being undone.
/// </remarks>
public sealed class ProgressTracker
{
	/// <summary>How much of the bar the script-writing phase takes.</summary>
	public const double ScriptShare = 0.1;

	private long _total;
	private long _done;
	private bool _forward = true;
	private bool _writingScript;
	private bool _stepPerActionData;
	private long _ticksPerActionData;

	/// <summary>Overall progress, 0 to 1.</summary>
	public double Fraction { get; private set; }

	/// <summary>Whether Windows Installer is undoing the changes (progress runs backwards).</summary>
	public bool IsRollingBack => !_forward;

	/// <summary>
	/// Applies an <c>INSTALLMESSAGE_PROGRESS</c> record's fields.
	/// </summary>
	/// <param name="field1">The progress type: 0 reset, 1 action info, 2 report, 3 add to total.</param>
	/// <param name="field2">The type's first value (ticks).</param>
	/// <param name="field3">The type's second value (reset: 0 forward, 1 backward; action info: 1 to step on ActionData).</param>
	/// <param name="field4">Reset only: 1 while writing the script, 0 while running it.</param>
	public void OnProgress(int field1, long field2, long field3, long field4)
	{
		switch (field1)
		{
			case 0:
				// Master reset: a new phase with a new total and direction.
				_total = Math.Max(0, field2);
				_forward = field3 == 0;
				_writingScript = field4 == 1;
				_done = _forward ? 0 : _total;
				_stepPerActionData = false;
				break;

			case 1:
				// Action info: whether each ActionData message that follows is a step, and how big.
				_stepPerActionData = field3 == 1;
				_ticksPerActionData = Math.Max(0, field2);
				break;

			case 2:
				// Progress report: ticks done since the last report.
				Advance(field2);
				break;

			case 3:
				// More ticks added to the phase's total (a custom action reserving its share).
				_total += Math.Max(0, field2);
				break;
		}

		Update();
	}

	/// <summary>Applies an <c>INSTALLMESSAGE_ACTIONDATA</c> message: a step, when the action asked for one.</summary>
	public void OnActionData()
	{
		if (_stepPerActionData)
		{
			Advance(_ticksPerActionData);
			Update();
		}
	}

	/// <summary>Applies an <c>INSTALLMESSAGE_ACTIONSTART</c> message: the previous action's ActionData steps end.</summary>
	public void OnActionStart() => _stepPerActionData = false;

	/// <summary>Fills the bar: the installation has ended.</summary>
	public void Complete() => Fraction = 1;

	private void Advance(long ticks) => _done += _forward ? ticks : -ticks;

	private void Update()
	{
		if (_total <= 0)
		{
			return;
		}

		double phase = Math.Max(0, Math.Min(1, (double)_done / _total));
		double overall = _writingScript
			? ScriptShare * phase
			: ScriptShare + ((1 - ScriptShare) * phase);

		// Forwards it never slips back (a phase's total can grow under it); a rollback runs down.
		Fraction = _forward ? Math.Max(Fraction, overall) : overall;
	}
}
