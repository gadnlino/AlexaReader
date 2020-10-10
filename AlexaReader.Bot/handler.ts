import { APIGatewayProxyHandler } from 'aws-lambda';
import 'source-map-support/register';
import Telegraf from "telegraf";
import Telegram from "telegraf/telegram";

export const hello: APIGatewayProxyHandler = async (event, _context) => {
  const requestBody = event.body[0] === '{' ?
    JSON.parse(event.body) :
    JSON.parse(Buffer.from(event.body, 'base64').toString());

  const bot = new Telegraf(process.env.BOT_TOKEN);

  // bot.telegram.setWebhook(process.env.APP_URL);
  // bot.startWebhook("/hello");

  bot.start(async (ctx) => {
    const instructions = `Olá!
    
      Para registrar seu email da Amazon, digite /email {seu_email}

      Para enviar um livro para a sua biblioteca, mande uma mensagem com o arquivo .epub para o bot`;

    await ctx.reply(instructions);
  });

  bot.hears("/email", async (ctx) => {
    await ctx.reply(ctx.update.message.text);
  });

  await bot.handleUpdate(requestBody);

  return {
    statusCode: 200,
    body: JSON.stringify({
      message: "",
    }),
  };
}
