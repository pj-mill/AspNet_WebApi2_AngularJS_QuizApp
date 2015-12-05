using GeekQuiz.Models;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace GeekQuiz.Controllers
{
    [Authorize]
    public class TriviaController : ApiController
    {
        private TriviaContext db = new TriviaContext();

        /// <summary>
        /// This method retrieves the following quiz question from the database to be answered by the specified user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        private async Task<TriviaQuestion> NextQuestionAsync(string userId)
        {
            var lastQuestionId = await this.db.TriviaAnswers
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.QuestionId)
            .Select(g => new { QuestionId = g.Key, Count = g.Count() })
            .OrderByDescending(q => new { QuestionId = q.QuestionId, q.Count })
            .Select(q => q.QuestionId)
            .FirstOrDefaultAsync();

            var questionsCount = await this.db.TriviaQuestions.CountAsync();

            var nextQuestionId = (lastQuestionId % questionsCount) + 1;
            return await this.db.TriviaQuestions.FindAsync(CancellationToken.None, nextQuestionId);
        }

        /// <summary>
        /// This method stores the specified answer in the database and returns a Boolean value indicating whether or not the answer is correct.
        /// </summary>
        /// <param name="answer"></param>
        /// <returns></returns>
        private async Task<bool> StoreAsync(TriviaAnswer answer)
        {
            this.db.TriviaAnswers.Add(answer);

            await this.db.SaveChangesAsync();

            var selectedOption = await this.db.TriviaOptions.FirstOrDefaultAsync(o => o.Id == answer.OptionId
                && o.QuestionId == answer.QuestionId);

            return selectedOption.IsCorrect;
        }


        // GET api/Trivia
        /// <summary>
        /// This action method calls the NextQuestionAsync helper method defined in the previous step to retrieve the next question for the authenticated user.
        /// </summary>
        /// <returns></returns>
        [ResponseType(typeof(TriviaQuestion))]
        public async Task<IHttpActionResult> Get()
        {
            var userId = User.Identity.Name;

            TriviaQuestion nextQuestion = await this.NextQuestionAsync(userId);

            if (nextQuestion == null)
            {
                return this.NotFound();
            }

            return this.Ok(nextQuestion);
        }

        /// <summary>
        /// This action method associates the answer to the authenticated user and calls the StoreAsync helper method. Then, it sends a response with the Boolean value returned by the helper method.
        /// </summary>
        /// <param name="answer"></param>
        /// <returns></returns>
        [ResponseType(typeof(TriviaAnswer))]
        public async Task<IHttpActionResult> Post(TriviaAnswer answer)
        {
            if (!ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            answer.UserId = User.Identity.Name;

            var isCorrect = await this.StoreAsync(answer);
            return this.Ok<bool>(isCorrect);
        }

        /// <summary>
        /// Ensure that all the resources used by the context object are released when the TriviaContext instance is disposed or garbage-collected. This includes closing all database connections opened by Entity Framework.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
