using Autofac;

namespace ECSDiscord.Services.Translations;

public class TranslationsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterInstance(Translator.DefaultTranslations).As<ITranslator>();
    }
}