import { SQS } from "aws-sdk";

const sqsClient = new SQS();


export default {
	sqs: {
		sendMessage: (queueUrl: string, body: string) => {
			const req = sqsClient.sendMessage({
				MessageBody: body,
				QueueUrl: queueUrl
			});

			return req.promise();
		}
	}
}