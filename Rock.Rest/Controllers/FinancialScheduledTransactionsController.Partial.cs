﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Rock.CodeGeneration project
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData;
using Rock;
using Rock.Data;
using Rock.Financial;
using Rock.Model;
using Rock.Rest.Filters;

namespace Rock.Rest.Controllers
{
    /// <summary>
    /// Additional methods for the FinancialPaymentDetails REST API
    /// </summary>
    public partial class FinancialScheduledTransactionsController
    {
        /// <summary>
        /// Gets Scheduled Transactions that have a Credit Card that is going to expire in the next X days (or has expired already)
        /// </summary>
        /// <param name="numberOfDays">The number of days from now</param>
        /// <param name="daysBack">The number of days ago. For example, if you don't want to include ones that have already expired, set daysBack=0.</param>
        /// <returns></returns>
        [Authenticate, Secured]
        [System.Web.Http.Route( "api/FinancialScheduledTransactions/GetExpiring" )]
        public List<FinancialScheduledTransaction> GetExpiring( int numberOfDays, int? daysBack = null )
        {
            // qry all ScheduledTransactions that have a FinancialPaymentDetail with an ExpirationMonth and Year
            var qry = this.Service.Queryable().Include( a => a.FinancialPaymentDetail ).Where( a => a.FinancialPaymentDetail.ExpirationMonthEncrypted != null && a.FinancialPaymentDetail.ExpirationYearEncrypted != null );

            //  fetch all the ScheduleTransactions into a list since ExpirationYear and ExpirationMonth are the decrypted from ExpirationMonthEncrypted and ExpirationYearEncrypted in C#
            var resultList = qry.ToList();

            var currentDate = RockDateTime.Now.Date;

            var expirationEndCutoff = currentDate.AddDays( numberOfDays );
            var expirationStartCutoff = daysBack.HasValue ? currentDate.AddDays( -daysBack.Value ) : DateTime.MinValue;
            var resultListWithExpiration = resultList.Select( a => new
            {
                a.FinancialPaymentDetail.ExpirationMonth,
                a.FinancialPaymentDetail.ExpirationYear,
                ExpirationDateTime = new DateTime( a.FinancialPaymentDetail.ExpirationYear.Value, a.FinancialPaymentDetail.ExpirationMonth.Value, 1 ).AddMonths( 1 ),
                FinancialScheduledTransaction = a,
            } );

            resultListWithExpiration = resultListWithExpiration.OrderBy( a => a.ExpirationDateTime );

            resultList = resultListWithExpiration
                .Where( a => a.ExpirationDateTime < expirationEndCutoff && a.ExpirationDateTime > expirationStartCutoff )
                .Select( a => a.FinancialScheduledTransaction ).ToList();

            return resultList;
        }

        /// <summary>
        /// Process and charge an instance of the scheduled transaction.
        /// </summary>
        /// <param name="scheduledTransactionId">The scheduled transaction identifier.</param>
        /// <param name="ignoreRepeatChargeProtection">If true, the payment will be charged even if there is a similar transaction for the same person within a short time period.</param>
        /// <param name="ignoreScheduleAdherenceProtection">If true, the payment will be charged even if the schedule has already been processed accoring to it's frequency.</param>
        /// <returns>The ID of the new transaction</returns>
        /// <exception cref="HttpResponseException"></exception>
        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/FinancialScheduledTransactions/Process/{scheduledTransactionId}" )]
        public System.Net.Http.HttpResponseMessage ProcessPayment( int scheduledTransactionId, [FromUri]bool ignoreRepeatChargeProtection, [FromUri]bool ignoreScheduleAdherenceProtection )
        {
            var financialScheduledTransactionService = Service as FinancialScheduledTransactionService;
            var financialScheduledTransaction = financialScheduledTransactionService.Queryable()
                .AsNoTracking()
                .Include( s => s.ScheduledTransactionDetails )
                .FirstOrDefault( t => t.Id == scheduledTransactionId );

            if ( financialScheduledTransaction == null )
            {
                var errorResponse = ControllerContext.Request.CreateErrorResponse( HttpStatusCode.NotFound, "The scheduledTransactionId did not resolve" );
                throw new HttpResponseException( errorResponse );
            }

            if ( !financialScheduledTransaction.FinancialGatewayId.HasValue )
            {
                var errorResponse = ControllerContext.Request.CreateErrorResponse( HttpStatusCode.BadRequest, "The scheduled transaction does not have an assigned gateway ID" );
                throw new HttpResponseException( errorResponse );
            }

            var details = financialScheduledTransaction.ScheduledTransactionDetails.Select( d =>
                new AutomatedPaymentArgs.AutomatedPaymentDetailArgs
                {
                    AccountId = d.AccountId,
                    Amount = d.Amount
                }
            ).ToList();

            var automatedPaymentArgs = new AutomatedPaymentArgs
            {
                ScheduledTransactionId = scheduledTransactionId,
                AuthorizedPersonAliasId = financialScheduledTransaction.AuthorizedPersonAliasId,
                AutomatedGatewayId = financialScheduledTransaction.FinancialGatewayId.Value,
                AutomatedPaymentDetails = details
            };

            var errorMessage = string.Empty;
            var rockContext = Service.Context as RockContext;

            var automatedPaymentProcessor = new AutomatedPaymentProcessor( GetPersonAliasId( rockContext ), automatedPaymentArgs, rockContext, ignoreRepeatChargeProtection, ignoreScheduleAdherenceProtection );

            if ( !automatedPaymentProcessor.AreArgsValid( out errorMessage ) ||
                automatedPaymentProcessor.IsRepeatCharge( out errorMessage ) ||
                !automatedPaymentProcessor.IsAccordingToSchedule( out errorMessage ) )
            {
                var errorResponse = ControllerContext.Request.CreateErrorResponse( HttpStatusCode.BadRequest, errorMessage );
                throw new HttpResponseException( errorResponse );
            }

            var transaction = automatedPaymentProcessor.ProcessCharge( out errorMessage );

            if ( !string.IsNullOrEmpty( errorMessage ) )
            {
                var errorResponse = ControllerContext.Request.CreateErrorResponse( HttpStatusCode.InternalServerError, errorMessage );
                throw new HttpResponseException( errorResponse );
            }

            if ( transaction == null )
            {
                var errorResponse = ControllerContext.Request.CreateErrorResponse( HttpStatusCode.InternalServerError, "No transaction was created" );
                throw new HttpResponseException( errorResponse );
            }

            var response = ControllerContext.Request.CreateResponse( HttpStatusCode.Created, transaction.Id );
            return response;
        }

        /// <summary>
        /// Returns an object for every active schedule ordered by the schedule's ID.
        /// Each object contains a Schedule and a MostRecentTransaction.
        /// The schedule has the FinancialPaymentDetail and ScheduledTransactionDetails objects expanded.
        /// </summary>
        /// <param name="pageSize">The number of records to return for each page</param>
        /// <param name="pageNumber">Zero-based page to return</param>
        /// <returns></returns>
        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/FinancialScheduledTransactions/WithPreviousTransaction" )]
        public System.Net.Http.HttpResponseMessage GetWithPreviousTransaction( [FromUri]int pageSize, [FromUri]int pageNumber )
        {
            var now = RockDateTime.Now;

            // Get all the active schedules on the page (determined by params) ordered by ID
            var schedules = Service.Queryable()
                .AsNoTracking()
                .Include( s => s.FinancialPaymentDetail )
                .Include( s => s.ScheduledTransactionDetails )
                .Where( s =>
                    s.IsActive &&
                    ( !s.NumberOfPayments.HasValue || s.Transactions.Count < s.NumberOfPayments ) &&
                    ( !s.EndDate.HasValue || s.EndDate >= now ) &&
                    s.StartDate <= now )
                .OrderBy( s => s.Id )
                .Skip( pageSize * pageNumber )
                .Take( pageSize )
                .ToList();

            // Extract the schedule IDs for the most recent transaction query
            var scheduleIds = schedules.Select( s => s.Id ).ToList();

            // Get the most recent transactions for each schedule
            var mostRecentTransactions = new FinancialTransactionService( Service.Context as RockContext ).Queryable()
                .AsNoTracking()
                .Where( t => t.ScheduledTransactionId.HasValue && scheduleIds.Contains( t.ScheduledTransactionId.Value ) )
                .GroupBy( t => t.ScheduledTransactionId )
                .Select( g => g.OrderByDescending( t => t.TransactionDateTime ).FirstOrDefault() )
                .ToDictionary( t => t.ScheduledTransactionId.Value, t => t );

            // Build an object for each schedule to return to the user  
            var schedulesWithMostRecentTransaction = schedules.Select( s => new
            {
                Schedule = s,
                MostRecentTransaction = mostRecentTransactions.GetValueOrNull( s.Id )
            } ).ToList();

            var response = ControllerContext.Request.CreateResponse( HttpStatusCode.OK, schedulesWithMostRecentTransaction );
            return response;
        }
    }
}
