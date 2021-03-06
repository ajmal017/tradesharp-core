/***************************************************************************** 
* Copyright 2016 Aurora Solutions 
* 
*    http://www.aurorasolutions.io 
* 
* Aurora Solutions is an innovative services and product company at 
* the forefront of the software industry, with processes and practices 
* involving Domain Driven Design(DDD), Agile methodologies to build 
* scalable, secure, reliable and high performance products.
* 
* TradeSharp is a C# based data feed and broker neutral Algorithmic 
* Trading Platform that lets trading firms or individuals automate 
* any rules based trading strategies in stocks, forex and ETFs. 
* TradeSharp allows users to connect to providers like Tradier Brokerage, 
* IQFeed, FXCM, Blackwood, Forexware, Integral, HotSpot, Currenex, 
* Interactive Brokers and more. 
* Key features: Place and Manage Orders, Risk Management, 
* Generate Customized Reports etc 
* 
* Licensed under the Apache License, Version 2.0 (the "License"); 
* you may not use this file except in compliance with the License. 
* You may obtain a copy of the License at 
* 
*    http://www.apache.org/licenses/LICENSE-2.0 
* 
* Unless required by applicable law or agreed to in writing, software 
* distributed under the License is distributed on an "AS IS" BASIS, 
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
* See the License for the specific language governing permissions and 
* limitations under the License. 
*****************************************************************************/


﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Documents;
using TraceSourceLogger;
using TradeHub.Common.Core.Constants;
using TradeHub.Common.Core.DomainModels;
using TradeHub.Common.Core.DomainModels.OrderDomain;

namespace TradeHub.StrategyRunner.Infrastructure.Entities
{
    /// <summary>
    /// Saves Strategy Statistics calculated upon executions
    /// </summary>
    public class Statistics
    {
        /// <summary>
        /// Strategy ID for which the stats are calculated
        /// </summary>
        private string _id;
        
        /// <summary>
        /// Strategy Profit/Loss
        /// </summary>
        private decimal _pnl;
        
        /// <summary>
        /// Total number of shares bought 
        /// </summary>
        private int _sharesBought;

        /// <summary>
        /// Total number of shares sold
        /// </summary>
        private int _sharesSold;

        /// <summary>
        /// Average Buy Price 
        /// </summary>
        private decimal _avgBuyPrice;

        /// <summary>
        /// Average Sell Price 
        /// </summary>
        private decimal _avgSellPrice;

        private decimal _pos = 0;
        private bool _flag = false;

        public bool Flag
        {
            get { return _flag; }
            set { _flag = value; }
        }

        /// <summary>
        /// Current Position
        /// </summary>
        public decimal Pos
        {
            get { return _pos; }
            set { _pos = value; }
        }

        #region Properties

        /// <summary>
        /// Strategy ID for which the stats are calculated
        /// </summary>
        public string ID
        {
            get { return _id; }
            set { _id = value; }
        }
        List<string> _pnlList=new List<string>();

        /// <summary>
        /// Gets Over all strategy position (Long/Short)
        /// </summary>
        public string Position
        {
            get
            {
                int temp = _sharesBought + (-_sharesSold);
                if (temp > 0)
                {
                    return "Long " + temp;
                }
                else if (temp < 0)
                {
                    return "Short " + Math.Abs(temp);
                }
                return "NONE";
            }
        }

        /// <summary>
        /// Strategy Profit/Loss
        /// </summary>
        public decimal Pnl
        {
            get { return _pnl; }
            set { _pnl = value; }
        }

        /// <summary>
        /// Total number of shares bought 
        /// </summary>
        public int SharesBought
        {
            get { return _sharesBought; }
            set { _sharesBought = value; }
        }

        /// <summary>
        /// Total number of shares sold
        /// </summary>
        public int SharesSold
        {
            get { return _sharesSold; }
            set { _sharesSold = value; }
        }

        /// <summary>
        /// Average Buy Price 
        /// </summary>
        public decimal AvgBuyPrice
        {
            get { return _avgBuyPrice; }
            set { _avgBuyPrice = value; }
        }

        /// <summary>
        /// Average Sell Price 
        /// </summary>
        public decimal AvgSellPrice
        {
            get { return _avgSellPrice; }
            set { _avgSellPrice = value; }
        }

        #endregion

        /// <summary>
        /// Argument Constructor
        /// </summary>
        /// <param name="id">Strategy id for which to calculate the statistics</param>
        public Statistics(string id)
        {
            _id = id;
            _pnl = default(decimal);
            _sharesBought = default(int);
            _sharesSold = default(int);
            _avgBuyPrice = default(decimal);
            _avgSellPrice = default(decimal);
        }

        //[MethodImpl(MethodImplOptions.Synchronized)]
        public void UpdatePnl(decimal pnl)
        {
            _pnlList.Add(pnl.ToString());
            var perpnl = pnl*15;
            if (perpnl < 0)
            {
                _negCount++;
                _sigmaTop += perpnl * perpnl;
            }
            else if (perpnl > 0)
            {
                _posCount++;
                _sigmaBottom += perpnl * perpnl;
            }
            _pnlSum += perpnl;
        }

        /// <summary>
        /// Resets fields to default values
        /// </summary>
        public void ResetValues()
        {
            _pnl = default(decimal);
            _sharesBought = default(int);
            _sharesSold = default(int);
            _avgBuyPrice = default(decimal);
            _avgSellPrice = default(decimal);
        }
        /// <summary>
        /// Update the PNL after receicing the execution
        /// </summary>
        /// <param name="execution"></param>
        public void UpdateCalulcationsOnExecutionMatlab(Execution execution)
        {
            // Update statistics on BUY Order
            if (execution.Fill.ExecutionSide.Equals(OrderSide.BUY))
            {
                //// Update Avg Buy Price
                //AvgBuyPrice = ((execution.Fill.ExecutionPrice * execution.Fill.ExecutionSize)
                //                           + (AvgBuyPrice * SharesBought))
                //                          / (SharesBought + execution.Fill.ExecutionSize);

                //// Update Size
                //SharesBought += execution.Fill.ExecutionSize;
                //Pnl = execution.Fill.ExecutionPrice - execution.BarClose;
            }
            // Update statistics on SELL Order
            else if (execution.Fill.ExecutionSide.Equals(OrderSide.SELL) ||
                execution.Fill.ExecutionSide.Equals(OrderSide.SHORT))
            {
                //// Update Avg Sell Price
                //AvgSellPrice = ((execution.Fill.ExecutionPrice * execution.Fill.ExecutionSize)
                //                            + (AvgSellPrice * SharesSold))
                //                           / (SharesSold + execution.Fill.ExecutionSize);

                //// Update Size
                //SharesSold += execution.Fill.ExecutionSize;
                //Pnl = execution.BarClose-execution.Fill.ExecutionPrice;
            }
            // Update statistics on COVER Order (order used to close the open position)
            else if (execution.Fill.ExecutionSide.Equals(OrderSide.COVER))
            {
                if (Position.Contains("Long"))
                {
                    //// Update Avg Sell Price
                    //AvgSellPrice = ((execution.Fill.ExecutionPrice * execution.Fill.ExecutionSize)
                    //                            + (AvgSellPrice * SharesSold))
                    //                           / (SharesSold + execution.Fill.ExecutionSize);

                    //// Update Size
                    //SharesSold += execution.Fill.ExecutionSize;
                    //Pnl = execution.Fill.ExecutionPrice - execution.BarClose;
                }
                else if (Position.Contains("Short"))
                {
                    //// Update Avg Buy Price
                    //AvgBuyPrice = ((execution.Fill.ExecutionPrice * execution.Fill.ExecutionSize)
                    //                           + (AvgBuyPrice * SharesBought))
                    //                          / (SharesBought + execution.Fill.ExecutionSize);

                    //// Update Size
                    //SharesBought += execution.Fill.ExecutionSize;
                    //Pnl = execution.BarClose - execution.Fill.ExecutionPrice;
                }
            }
            Pnl = Pnl*15;

            // Update Profit and Loss
            //Pnl = (AvgSellPrice * SharesSold) -
            //                  (AvgBuyPrice * SharesBought);
            UpdateCalcualtions();
        }
        /// <summary>
        /// Update the PNL after receicing the execution
        /// </summary>
        /// <param name="execution"></param>
        public void UpdateCalulcationsOnExecution(Execution execution)
        {
            // Update statistics on BUY Order
            if (execution.Fill.ExecutionSide.Equals(OrderSide.BUY))
            {
                // Update Avg Buy Price
                AvgBuyPrice = ((execution.Fill.ExecutionPrice * execution.Fill.ExecutionSize)
                                           + (AvgBuyPrice * SharesBought))
                                          / (SharesBought + execution.Fill.ExecutionSize);

                // Update Size
                SharesBought += execution.Fill.ExecutionSize;
            }
            // Update statistics on SELL Order
            else if (execution.Fill.ExecutionSide.Equals(OrderSide.SELL) ||
                execution.Fill.ExecutionSide.Equals(OrderSide.SHORT))
            {
                // Update Avg Sell Price
                AvgSellPrice = ((execution.Fill.ExecutionPrice * execution.Fill.ExecutionSize)
                                            + (AvgSellPrice * SharesSold))
                                           / (SharesSold + execution.Fill.ExecutionSize);

                // Update Size
                SharesSold += execution.Fill.ExecutionSize;
            }
            // Update statistics on COVER Order (order used to close the open position)
            else if (execution.Fill.ExecutionSide.Equals(OrderSide.COVER))
            {
                if (Position.Contains("Long"))
                {
                    // Update Avg Sell Price
                    AvgSellPrice = ((execution.Fill.ExecutionPrice * execution.Fill.ExecutionSize)
                                                + (AvgSellPrice * SharesSold))
                                               / (SharesSold + execution.Fill.ExecutionSize);

                    // Update Size
                    SharesSold += execution.Fill.ExecutionSize;
                }
                else if (Position.Contains("Short"))
                {
                    // Update Avg Buy Price
                    AvgBuyPrice = ((execution.Fill.ExecutionPrice * execution.Fill.ExecutionSize)
                                               + (AvgBuyPrice * SharesBought))
                                              / (SharesBought + execution.Fill.ExecutionSize);

                    // Update Size
                    SharesBought += execution.Fill.ExecutionSize;
                }
            }

            // Update Profit and Loss
            Pnl = (AvgSellPrice * SharesSold) -
                              (AvgBuyPrice * SharesBought);
            UpdateCalcualtions();
        }

        #region New Enhancements in Utility Funtion

        private decimal _sigmaTop=0;
        private decimal _sigmaBottom = 0;
        private decimal _pnlSum = 0;
        private decimal _risk = 0.5m;
        private int _negCount = 0;
        private int _posCount = 0;
        private Bar _currentBar;
        private Bar _prevBar;

        public void UpdateBar(Bar bar)
        {
            //Flag = false;
            _prevBar = _currentBar;
            _currentBar = bar;
        }

        public void CalculatePnlAfterBar()
        {
            Flag = false;
            if (!Flag)
            {
                decimal perpnl = 0;
                if (Pos > 0)
                {
                    perpnl = (_currentBar.Close - _prevBar.Close) * Pos;
                    UpdatePnl(perpnl);
                }
                else if (Pos < 0)
                {
                    perpnl = (_currentBar.Close - _prevBar.Close) * Pos;
                    UpdatePnl(perpnl);
                }
            }
        }
        //[MethodImpl(MethodImplOptions.Synchronized)]
        public void MatlabStatisticsFunction(Execution execution)
        {
            decimal perpnl = 0;
            if (Pos == 0)
            {
                if (execution.Fill.ExecutionSide.Equals(Common.Core.Constants.OrderSide.SELL) ||
                    execution.Fill.ExecutionSide.Equals(Common.Core.Constants.OrderSide.SHORT))
                {
                    Pos = -1;
                    perpnl = (execution.Fill.ExecutionPrice - _currentBar.Close) * -1 * Pos;
                    Flag = true;
                }
                else if (execution.Fill.ExecutionSide.Equals(Common.Core.Constants.OrderSide.BUY))
                {
                    Pos = 1;
                    perpnl = (_currentBar.Close - execution.Fill.ExecutionPrice) * Pos;
                    Flag = true;
                }
            }
            if (!Flag)
            {
                if (Pos < 0)
                {
                    if (execution.Order.Remarks.Contains("PT-3"))
                    {
                        perpnl = (_prevBar.Close - execution.Fill.ExecutionPrice)*-1*Pos;
                        Pos = 0;
                    }
                    else if (execution.Order.Remarks.Contains("PT"))
                    {
                        Pos += 0.33m;
                        var gl = _prevBar.Close - execution.Fill.ExecutionPrice;
                        perpnl = gl - (_currentBar.Close - _prevBar.Close) * -1 * Pos;
                    }
                    else
                    {
                        perpnl = (_prevBar.Close - execution.Fill.ExecutionPrice) * -1 * Pos;
                        Pos = 0;
                    }
                }
                else if (Pos > 0)
                {
                    if (execution.Order.Remarks.Contains("PT-3"))
                    {
                        perpnl = (execution.Fill.ExecutionPrice-_prevBar.Close) * Pos;
                        Pos = 0;
                    }
                    else if (execution.Order.Remarks.Contains("PT"))
                    {
                        Pos -= 0.33m;
                        var gl = execution.Fill.ExecutionPrice - _prevBar.Close;
                        perpnl = gl + (_currentBar.Close - _prevBar.Close) * Pos;
                    }
                    else
                    {
                        perpnl = (execution.Fill.ExecutionPrice - _prevBar.Close) * Pos;
                        Pos = 0;
                    }
                }
            }
            UpdatePnl(perpnl);
        }
        /// <summary>
        /// Update calcualtions
        /// </summary>
        public void UpdateCalcualtions()
        {
            //Check if the position becomes flat
            if (Position.Contains("NONE"))
            {
                if (Pnl < 0)
                {
                    _negCount++;
                    _sigmaTop += _pnl*_pnl;
                }
                else if (Pnl > 0)
                {
                    _posCount++;
                    _sigmaBottom += _pnl * _pnl;
                }
                _pnlSum += Pnl;
                //reset values as the position is zero now.
                ResetValues();
            }
            
        }
        
        /// <summary>
        /// Get Risk after the iteration
        /// </summary>
        /// <returns></returns>
        public decimal GetRisk()
        {
            decimal risk = 0;
            decimal sigma = 0;
            //decimal temp = 0;
            if (_sigmaTop != 0 && _sigmaBottom != 0)
            {
                sigma = _sigmaTop / _sigmaBottom;
            }
            //temp = _pnlSum/(_negCount + _posCount);
            //Logger.Error("Stats, Sigma Top="+_sigmaTop+", Sigma Bottom="+_sigmaBottom+", Sigma="+sigma+", Pnl Sum="+_pnlSum,"Statistics","GetRisk");
            risk = +1 * (((1 - _risk) * _pnlSum) - _risk * sigma);
            _pnlList.Add("R_Hat="+_pnlSum);
            _pnlList.Add("utility=" + risk);
            File.WriteAllLines("pnlList.txt", _pnlList);
            return risk;
        }

        /// <summary>
        /// Reset all and new values
        /// </summary>
        public void ResetAllValues()
        {
            ResetValues();
            _sigmaTop = 0;
            _sigmaBottom = 0;
            _pnlSum = 0;
            _risk = 0.5m;
            _negCount = 0;
            _posCount = 0;
            _pos = 0;
            _flag = false;
        }

        #endregion

        /// <summary>
        /// ToString override for Statistics.cs
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("Statistics :: ");
            stringBuilder.Append("ID: " + _id);
            stringBuilder.Append(" | Position: " + Position);
            stringBuilder.Append(" | PNL: " + _pnl);
            stringBuilder.Append(" | Bought: " + _sharesBought);
            stringBuilder.Append(" | Sold: " + _sharesSold);
            stringBuilder.Append(" | Avg Buy Price: " + _avgBuyPrice);
            stringBuilder.Append(" | Avg Sell Price: " + _avgSellPrice);

            return stringBuilder.ToString();
        }
    }
}
