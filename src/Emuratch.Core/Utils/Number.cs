namespace Emuratch.Core.Utils;

public struct Number
{
	static PrecisionMode Default = PrecisionMode.Double;
	public static void SetDefaultPrecision(PrecisionMode precision)
	{
		Default = precision;
	}

	public Number(float value)
	{
		precision = Default;
		this.value = 0;

		this.value = precision switch
		{
			PrecisionMode.Double => (double)value,
			_ => value
		};
	}

	public Number(double value)
	{
		precision = Default;
		this.value = 0;

		this.value = precision switch
		{
			PrecisionMode.Float => (float)value,
			_ => value
		};
	}

	public Number()
	{
		precision = Default;
		value = 0;

		switch (precision)
		{
			case PrecisionMode.Float:
				value = 0f;
				break;

			case PrecisionMode.Double:
				value = 0d;
				break;
		}
	}

	public enum PrecisionMode
	{
		Double,
		Float,
	}

	public PrecisionMode precision;
	public dynamic value
	{
		get => precision switch
		{
			PrecisionMode.Float => fval,
			PrecisionMode.Double => dval,
			_ => dval
		};

		set
		{
			switch (precision)
			{
				case PrecisionMode.Float:
					fval = (float)value;
					break;

				case PrecisionMode.Double:
					dval = (double)value;
					break;
			}
		}
	}

	float fval = 0;
	double dval = 0;

	public static bool TryParse(string s, out Number n)
	{
		if (int.TryParse(s, out var i))
		{
			n = i;
			return true;
		}
		else if (double.TryParse(s, out var d))
		{
			n = d;
			return true;
		}
		else
		{
			switch (s)
			{
				case "Infinity":
					n = new(double.PositiveInfinity);
					return true;

				case "-Infinity":
					n = new(double.NegativeInfinity);
					return true;

				default:
					n = new(0);
					return false;
			}
		}
	}

	public static Number operator +(Number a, Number b) => new(a.value + b.value);
	public static Number operator -(Number a, Number b) => new(a.value - b.value);
	public static Number operator *(Number a, Number b) => new(a.value * b.value);
	public static Number operator /(Number a, Number b) => new(a.value / b.value);
	public static Number operator ^(Number a, Number b) => new(a.value ^ b.value);
	public static Number operator %(Number a, Number b) => new(a.value % b.value);

	public override string ToString()
	{
		return ((double)value).ToString();
	}
	public static implicit operator double(Number n) => (double)n.value;
	public static implicit operator float(Number n) => (float)n.value;
	public static implicit operator int(Number n) => (int)n.value;
	public static implicit operator Number(int i) => new(i);
	public static implicit operator Number(float f) => new(f);
	public static implicit operator Number(double d) => new(d);
	public static implicit operator Number(string s)
	{
		if (TryParse(s, out var n))
		{
			return n;
		}
		else
		{
			return 0;
		}
	}
}