import { APIGatewayProxyHandler } from 'aws-lambda';
import 'source-map-support/register';
import Telegraf from "telegraf";
import awsService from "./awsService";
import { EpubDownloadContract } from './models';


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

  bot.command("email", async (ctx) => {
    await ctx.reply(ctx.update.message.text);
  });

  bot.on("document", async (ctx) => {
    const { document } = ctx.message;

    console.log(document);

    if (document.mime_type !== "application/epub+zip") {
      await ctx.reply("Formato de arquivo inválido. Por favor, faça upload de um arquivo .epub");
    }
    else if (document.file_size > 10485760) {
      await ctx.reply("Arquivo excedeu o limite de 10 MB");
    }
    else {
      // const response = await
      //   axios.get(`https://api.telegram.org/bot${process.env.BOT_TOKEN}/getFile?file_id=${document.file_id}`);

      // const { file_path } = response.data.result;

      // console.log(file_path);
      var contract: EpubDownloadContract = {
        FileId: document.file_id,
        Person:{
          FromId: ctx.message.from.id.toString(),
          FirstName: ctx.message.from.first_name,
          LastName: ctx.message.from.last_name
        }
      };

      await awsService.sqs.sendMessage(process.env.FILE_DOWNLOADER_QUEUE_URL,
        JSON.stringify(contract));
      await ctx.reply("Epub enviado com sucesso!");
      await ctx.reply("O livro estará disponível na sua biblioteca em breve.");
    }
  });

  await bot.handleUpdate(requestBody);

  return {
    statusCode: 200,
    body: JSON.stringify({
      message: "",
    }),
  };
}
