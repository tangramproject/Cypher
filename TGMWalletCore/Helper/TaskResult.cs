// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;

namespace TGMWalletCore.Helper
{
    public class TaskResult<T>
    {
        private TaskResult()
        {

        }

        public bool Success { get; private set; }
        public T Result { get; private set; }
        public dynamic NonSuccessMessage { get; private set; }
        public Exception Exception { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static TaskResult<T> CreateSuccess(T result)
        {
            return new TaskResult<T> { Success = result != null, Result = result };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="successMessage"></param>
        /// <returns></returns>
        public static TaskResult<T> CreateSuccess(dynamic successMessage)
        {
            return new TaskResult<T> { Success = successMessage != null, Result = successMessage };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nonSuccessMessage"></param>
        /// <returns></returns>
        public static TaskResult<T> CreateFailure(dynamic nonSuccessMessage)
        {
            return new TaskResult<T> { Success = false, Result = default, NonSuccessMessage = nonSuccessMessage };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static TaskResult<T> CreateFailure(Exception ex)
        {
            return new TaskResult<T>
            {
                Success = false,
                NonSuccessMessage = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}",
                Exception = ex,
                Result = default,
            };
        }
    }
}