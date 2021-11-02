using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Garth.Models;
using Garth.Services;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;

namespace Garth.Modules
{
    public class EvalModule : ModuleBase<SocketCommandContext>
    {
        public Configuration? _configuration { get; set; }
        public ReplyTrackerService? _replyTracker { get; set; }

        private async Task<object> Evaluate(string input)
        {
            var options = ScriptOptions.Default
                .AddReferences(new Assembly[]
                {
                    typeof(string).GetTypeInfo().Assembly,
                    typeof(Assembly).GetTypeInfo().Assembly,
                    typeof(Task).GetTypeInfo().Assembly,
                    typeof(Enumerable).GetTypeInfo().Assembly,
                    typeof(List<>).GetTypeInfo().Assembly,
                    typeof(IGuild).GetTypeInfo().Assembly,
                    typeof(SocketGuild).GetTypeInfo().Assembly,
                    typeof(Task<>).GetTypeInfo().Assembly,
                    typeof(File).GetTypeInfo().Assembly,
                    typeof(Math).GetTypeInfo().Assembly,
                    typeof(JsonConvert).GetTypeInfo().Assembly
                })
                .AddImports(new string[]
                {
                    "System",
                    "System.Reflection",
                    "System.Threading.Tasks",
                    "System.Linq",
                    "System.Collections.Generic",
                    "Discord",
                    "Discord.WebSocket",
                    "System.Threading",
                    "System.IO",
                    "System.Math",
                    "Newtonsoft.Json"
                });

            return await CSharpScript.EvaluateAsync(input, options, new ScriptHost(Context));
        }

        [Command("eval"), Description("Evaluates C# code.")]
        [Alias("e")]
        public async Task Eval([Remainder] string input)
        {
            if (_configuration.BotOwnerId != Context.User.Id)
                return;

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                var result = await Evaluate(input);

                sw.Stop();

                EmbedBuilder builder = new EmbedBuilder()
                    .WithTitle("Roslyn Compiler Output Success")
                    .WithDescription($"```\n{result}\n```")
                    .WithFooter(
                        new EmbedFooterBuilder()
                            .WithText($"Compile time: {sw.ElapsedMilliseconds}ms")
                    )
                    .WithTimestamp(DateTime.Now)
                    .WithColor(new Color(85, 217, 76));

                await _replyTracker.SmartReplyAsync(Context, "", embed: builder.Build());
            }
            catch (Exception ex)
            {
                EmbedBuilder builder = new EmbedBuilder()
                    .WithTitle("Roslyn Compiler Output Failed")
                    .WithDescription($"```\n{ex.ToString()}\n```")
                    .WithTimestamp(DateTime.Now)
                    .WithColor(new Color(235, 52, 58));

                await _replyTracker.SmartReplyAsync(Context, "", embed: builder.Build());
            }
        }

        [Command("inspect"), Description("Evaluates C# object")]
        [Alias("ins")]
        public async Task Inspect([Remainder] string input)
        {
            if (_configuration.BotOwnerId != Context.User.Id)
                return;
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                var result = await Evaluate(input);
                var json = System.Text.Json.JsonSerializer.Serialize(result).Substring(0, Math.Min(result.ToString().Length, 2036));

                sw.Stop();

                EmbedBuilder builder = new EmbedBuilder()
                    .WithTitle("Roslyn Compiler Output Success")
                    .WithDescription($"```json\n{result}\n```")
                    .WithFooter(
                        new EmbedFooterBuilder()
                            .WithText($"Compile time: {sw.ElapsedMilliseconds}ms")
                    )
                    .WithTimestamp(DateTime.Now)
                    .WithColor(new Color(85, 217, 76));

                await _replyTracker.SmartReplyAsync(Context, "", embed: builder.Build());
            }
            catch (Exception ex)
            {
                EmbedBuilder builder = new EmbedBuilder()
                    .WithTitle("Roslyn Compiler Output Failed")
                    .WithDescription($"```\n{ex.ToString()}\n```")
                    .WithTimestamp(DateTime.Now)
                    .WithColor(new Color(235, 52, 58));

                await _replyTracker.SmartReplyAsync(Context, "", embed: builder.Build());
            }
        }
    }
}