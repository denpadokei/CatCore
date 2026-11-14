using DryIoc;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	/// <summary>
	/// Verifies the DryIoc registration pattern that CatCore uses to expose a single service
	/// instance through several service interfaces.
	/// </summary>
	public class DryIocMultiServiceRegistrationTests
	{
		public interface IFirstService
		{
		}

		public interface ISecondService
		{
		}

		public interface IThirdService
		{
		}

		internal sealed class MultiService : IFirstService, ISecondService, IThirdService
		{
		}

		[Fact]
		public void RegisterMany_ResolvesTheSameSingletonInstanceForEveryServiceType()
		{
			var container = new Container();
			container.RegisterMany(new[] { typeof(IFirstService), typeof(ISecondService), typeof(IThirdService) }, typeof(MultiService), Reuse.Singleton);

			var first = container.Resolve<IFirstService>();
			var second = container.Resolve<ISecondService>();
			var third = container.Resolve<IThirdService>();

			first.Should().BeSameAs(second);
			second.Should().BeSameAs(third);
		}
	}
}
