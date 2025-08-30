using Emuratch.Core.Utils;

namespace Emuratch.Core.Test;

public class NumberTest
{
	[Fact]
	public void Operations()
	{
		Number.SetDefaultPrecision(Number.PrecisionMode.Double);
		{
			Number a = 3.25;
			Number b = 6.8;
			Assert.Equal(10.05, (double)(a + b), 7);
		}
		{
			Number a = 3;
			Number b = 7;
			Assert.Equal(10, (int)(a + b));
		}
		{
			Number a = "3.25";
			Number b = "6.8";
			Assert.Equal(10, (int)(a + b));
		}
		Number.SetDefaultPrecision(Number.PrecisionMode.Float);
		{
			Number a = 3.25f;
			Number b = 6.8f;
			Assert.Equal(10.05f, (float)(a + b), 10);
		}
		{
			Number a = 3;
			Number b = 7;
			Assert.Equal(10, (int)(a + b));
		}
		{
			Number a = "3.25";
			Number b = "6.8";
			Assert.Equal(10, (int)(a + b));
		}
	}
}
