namespace ConsoleApp3.Models
{
	public class ClassWithPrivateConstructor
	{
		private readonly string _state;

		private ClassWithPrivateConstructor(string state)
		{
			_state = state;
		}

		public string GetState()
		{
			return "My state can never be null: " + (_state ?? "... Or can it?");
		}
	}
}
