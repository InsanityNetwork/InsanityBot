﻿using System;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using InsanityBot.Utility.Modlogs;
using InsanityBot.Utility.Modlogs.Reference;
using InsanityBot.Utility.Permissions;

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

using static System.Convert;
using static InsanityBot.Commands.StringUtilities;

namespace InsanityBot.Commands.Moderation
{
    // supports command line argument syntax :blobaww:
    public class Warn : BaseCommandModule
    {
        [Command("warn")]
        public async Task WarnCommand(CommandContext ctx,
            DiscordMember target,

            [RemainingText]
            String arguments = "usedefault")
        {
            if (arguments.StartsWith('-'))
            {
                await ParseWarnSyntax(ctx, target, arguments);
                return;
            }
            await ExecuteWarn(ctx, target, arguments, false, false);
        }

        private async Task ParseWarnSyntax(CommandContext ctx,
            DiscordMember target,
            String arguments)
        {
            //create the command parser
            CommandLineApplication WarnSyntaxParser = new CommandLineApplication(false);

            CommandOption ReasonOption = WarnSyntaxParser.Option("--reason|-r", "nothing since this isnt visible anyways", CommandOptionType.SingleValue);
            CommandOption SilentOption = WarnSyntaxParser.Option("--silent|-s", "nothing since this isnt visible either", CommandOptionType.NoValue);
            CommandOption DmMemberOption = WarnSyntaxParser.Option("--dmmember|-dm", "nothing, why would there", CommandOptionType.NoValue);

            WarnSyntaxParser.Execute(arguments);

            Boolean Silent = SilentOption.Value() == "on";
            Boolean DmMember = DmMemberOption.Value() == "on";
            String Reason = ReasonOption.Value() ?? InsanityBot.LanguageConfig["insanitybot.moderation.no_reason_given"];

            await ExecuteWarn(ctx, target, Reason, Silent, DmMember);
        }

        private async Task ExecuteWarn(CommandContext ctx,
            DiscordMember target,
            String reason,
            Boolean silent, 
            Boolean dmMember)
        {
            if (!ctx.Member.HasPermission("insanitybot.moderation.warn"))
            {
                await ctx.RespondAsync(InsanityBot.LanguageConfig["insanitybot.error.lacking_permission"]);
                return;
            }

            //if silent - delete the warn command
            if (silent)
                await ctx.Message.DeleteAsync();

            //actually do something with the usedefault value
            String WarnReason = reason switch
            {
                "usedefault" => GetFormattedString(InsanityBot.LanguageConfig["insanitybot.moderation.no_reason_given"],
                                ctx, target),
                _ => GetFormattedString(reason, ctx, target)
            };

            DiscordEmbedBuilder embedBuilder = null;

            DiscordEmbedBuilder moderationEmbedBuilder = new DiscordEmbedBuilder
            {
                Title = "Warn",
                Color = DiscordColor.Yellow,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "InsanityBot - ExaInsanity 2020"
                }
            };

            moderationEmbedBuilder.AddField("Moderator", ctx.Member.Mention, true)
                .AddField("Member", target.Mention, true)
                .AddField("Reason", WarnReason, true);

            try
            {
                target.AddModlogEntry(ModlogEntryType.warn, WarnReason);
                embedBuilder = new DiscordEmbedBuilder
                {
                    Description = GetFormattedString(InsanityBot.LanguageConfig["insanitybot.moderation.warn.success"],
                        ctx, target),
                    Color = DiscordColor.Red,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "InsanityBot - ExaInsanity 2020"
                    }
                };
                _ = InsanityBot.HomeGuild.GetChannel(ToUInt64(InsanityBot.Config["insanitybot.identifiers.commands.modlog_channel_id"]))
                    .SendMessageAsync(embed: moderationEmbedBuilder.Build());
            }
            catch (Exception e)
            {
                embedBuilder = new DiscordEmbedBuilder
                {
                    Description = GetFormattedString(InsanityBot.LanguageConfig["insanitybot.moderation.warn.failure"],
                        ctx, target),
                    Color = DiscordColor.Red,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "InsanityBot - ExaInsanity 2020"
                    }
                };
                InsanityBot.Client.Logger.LogError($"{e}: {e.Message}");
            }
            finally
            {
                if(!silent)
                    await ctx.RespondAsync(embed: embedBuilder.Build());
                if(dmMember)
                {
                    embedBuilder.Description = GetReason(InsanityBot.LanguageConfig["insanitybot.moderation.warn.reason"], WarnReason);
                    await (await target.CreateDmChannelAsync()).SendMessageAsync(embed: embedBuilder.Build());
                }
            }
        }
    }
}