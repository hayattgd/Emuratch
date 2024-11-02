using Emuratch.Core.Scratch;

namespace Emuratch.Core.vm;

public interface Executer
{
    public Sprite Sprite { get; set; }
    public void Execute(Block block);
}