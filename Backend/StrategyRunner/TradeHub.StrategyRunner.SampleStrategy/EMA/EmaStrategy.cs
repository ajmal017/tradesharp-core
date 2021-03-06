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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TraceSourceLogger;
using TradeHub.Common.Core.CustomAttributes;
using TradeHub.Common.Core.DomainModels;
using TradeHub.Common.Core.DomainModels.OrderDomain;
using TradeHub.Common.Core.FactoryMethods;
using TradeHub.StrategyEngine.TradeHub;
using TradeHub.StrategyRunner.SampleStrategy.Utility;
using TradeHubConstants = TradeHub.Common.Core.Constants;

namespace TradeHub.StrategyRunner.SampleStrategy.EMA
{
    /// <summary>
    /// Implements Basic 2EMA strategy using TradeHubStrategy as base skeleton
    /// </summary>
    [TradeHubAttributes("EMA strategy", typeof(EmaStrategy))]
    public class EmaStrategy : TradeHubStrategy
    {
        private readonly Type _type = typeof(EmaStrategy);

        private readonly string _emaPriceType;
        private readonly string _symbol;
        private readonly string _barFormat;
        private readonly string _barPriceType;
        private readonly string _marketDataProvider;
        private readonly string _orderExecutionProvider;

        private decimal _barLength;

        private int _shortEma;
        private int _longEma;
        private int _currentEntryState;
        private int _previousEntryState;
        private int _liveBarId = 0xA00;
        private int _tickDataId = 0xA00;
        private int _orderId = 0xA00;

        private bool _entryOrderSent;
        private bool _exitOrderSent;

        private readonly Indicator.EMA _emaCalculator;
        private int _count = 0;

        [TradeHubAttributes("Lenght of the bar to be used", typeof(decimal))]
        public decimal BarLength
        {
            get { return _barLength; }
            set { _barLength = value; }
        }

        [TradeHubAttributes("Short EMA value", typeof(int))]
        public int ShortEma
        {
            get { return _shortEma; }
            set { _shortEma = value; }
        }

        [TradeHubAttributes("Long EMA value", typeof(int))]
        public int LongEma
        {
            get { return _longEma; }
            set { _longEma = value; }
        }

        /// <summary>
        /// Argument Constructor
        /// </summary>
        /// <param name="shortEma">Value for short EMA</param>
        /// <param name="longEma">Value for Long EMA</param>
        /// <param name="emaPriceType">Price type to be used for EMA Calculations</param>
        /// <param name="symbol">Symbol on which to run the strategy</param>
        /// <param name="barLength">Length of the bar to be used for EMA</param>
        /// <param name="barFormat">Bar generator formate to be used</param>
        /// <param name="barPriceType">Price type to be used for creating bars</param>
        /// <param name="marketDataProvider">Name of market data provider to be used</param>
        /// <param name="orderExecutionProvider">Name of order execution provider to be used</param>
        public EmaStrategy(int shortEma, int longEma, string emaPriceType, string symbol, decimal barLength,
                                                string barFormat, string barPriceType, string marketDataProvider, string orderExecutionProvider)
                                                                                            : base(marketDataProvider, orderExecutionProvider, "")
        {
            _shortEma = shortEma;
            _longEma = longEma;
            _emaPriceType = emaPriceType;
            _symbol = symbol;
            _barLength = barLength;
            _barFormat = barFormat;
            _barPriceType = barPriceType;
            _marketDataProvider = marketDataProvider;
            _orderExecutionProvider = orderExecutionProvider;

            _emaCalculator = new Indicator.EMA(shortEma, longEma, _emaPriceType);
        }

        /// <summary>
        /// Starts Strategy Execution
        /// </summary>
        protected override void OnRun()
        {
            if (!IsRunning)
            {
                // Indidicates strategy is running
                base.Run();

                // Send subscription request for tick data
                SubscribeTickData();

                // Send subscription request for live bars
                // SubscribeLiveBars();
            }
        }

        /// <summary>
        /// Stops Strategy Execution
        /// </summary>
        protected override void OnStop()
        {
            if (IsRunning)
            {
                // Send unsubscription request for tick data
                UnsubscribeTickData();

                // Send unsubscription request for live bars
                // UnsubscribeLiveBars();
            }
        }

        /// <summary>
        /// Sends live bar subscription request to Market Data Service using the base class
        /// </summary>
        public void SubscribeLiveBars()
        {
            try
            {
                // Get new ID to be used
                var id = (_liveBarId++).ToString("X");

                // Get new Security object
                Security security = new Security {Symbol = _symbol};

                // Get Bar subscription message
                var subscribe = SubscriptionMessage.LiveBarSubscription(id, security, _barFormat, _barPriceType,
                                                                        BarLength, 0.0001M, 0, _marketDataProvider);

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Sending live bar subscription request for: " + subscribe, _type.FullName,
                                "SubscribeLiveBars");
                }

                // Send subscription request
                Subscribe(subscribe);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "SubscribeLiveBars");
            }
        }

        /// <summary>
        /// Sends tick data subscription request to Market Data Service using the base class
        /// </summary>
        public void SubscribeTickData()
        {
            try
            {
                // Get new ID to be used
                var id = (_tickDataId++).ToString("X");

                // Get new Security object
                Security security = new Security {Symbol = _symbol};

                // Get Tick subscription message
                var subscribe = SubscriptionMessage.TickSubscription(id, security, _marketDataProvider);

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Sending tick data subscription request for: " + subscribe, _type.FullName,
                                "SubscribeTickData");
                }

                // Send subscription request
                Subscribe(subscribe);

                // Start Timer
                ConnectivityTimer.Start();
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "SubscribeTickData");
            }
        }

        /// <summary>
        /// Sends tick data unsubscription request to Market Data Service using the base class
        /// </summary>
        public void UnsubscribeTickData()
        {
            try
            {
                // Get new ID to be used
                var id = (_tickDataId++).ToString("X");

                // Get new Security object
                Security security = new Security { Symbol = _symbol };

                // Get Tick subscription message
                var subscribe = SubscriptionMessage.TickUnsubscription(id, security, _marketDataProvider);

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Sending tick data unsubscription request for: " + subscribe, _type.FullName,
                                "UnsubscribeTickData");
                }

                // Send subscription request
                Unsubscribe(subscribe);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "UnsubscribeTickData");
            }
        }

        /// <summary>
        /// Initiates Trading by sending out Entry Order
        /// </summary>
        /// <param name="state"> </param>
        public void InitiateTrade(object state)
        {
            try
            {
                lock (state)
                {
                    Bar bar = state as Bar;

                    if (bar == null) return;

                    if (Logger.IsDebugEnabled)
                    {
                        Logger.Debug("Initiating Trade Algo", _type.FullName, "InitiateTrade");
                    }

                    decimal[] ema = _emaCalculator.GetEMA(bar);

                    if (ema[0] > 0 && ema[1] > 0)
                    {
                        ConsoleWriter.WriteLine(ConsoleColor.Green,
                                                "New EMA calulated: Long = " + ema[0] + " | Short = " + ema[1]);

                        if (Logger.IsDebugEnabled)
                        {
                            Logger.Debug("New EMA calulated: Long = " + ema[0] + " | Short = " + ema[1], _type.FullName,
                                         "InitiateTrade");
                        }

                        //Update Entry States using latest EMA
                        ManageEntryStates(ema[0], ema[1]);

                        // Check conditions if Entry Order is yet to be sent
                        if (!_entryOrderSent && !_exitOrderSent)
                        {
                            // Check for ENTRY Condition
                            var orderSide = EntrySignal();
                            if (Logger.IsDebugEnabled)
                            {
                                Logger.Debug("New entry signal generated: " + orderSide, _type.FullName, "InitiateTrade");
                            }

                            ConsoleWriter.WriteLine(ConsoleColor.Green, "New entry signal generated: " + orderSide);

                            // Send Entry Order
                            SendEntryOrder(orderSide, bar.Security.Symbol);
                        }

                        // Update previous state value
                        _previousEntryState = _currentEntryState;
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "InitiateTrade");
            }
        }

        /// <summary>
        /// Manage Entry States
        /// </summary>
        /// <param name="currentLongEMA">newly calculated Long EMA</param>
        /// <param name="currentShortEMA">newly calculated Short EMA</param>
        private void ManageEntryStates(decimal currentLongEMA, decimal currentShortEMA)
        {
            try
            {
                if (currentShortEMA > currentLongEMA)
                {
                    _currentEntryState = 1;
                }
                else if (currentShortEMA < currentLongEMA)
                {
                    _currentEntryState = -1;
                }

                ConsoleWriter.WriteLine(ConsoleColor.Green,
                                                "Previous Entry State: " + _previousEntryState + " | Current Entry State: " + _currentEntryState);

                if (Logger.IsDebugEnabled)
                {
                    Logger.Debug("Previous Entry State: " + _previousEntryState + " | Current Entry State: " + _currentEntryState,
                                    _type.FullName, "ManageEntryStates");
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "ManageEntryStates");
            }
        }

        /// <summary>
        /// Checks the entry states and  generates Entry signal if required
        /// </summary>
        public string EntrySignal()
        {
            try
            {
                string orderSide = TradeHubConstants.OrderSide.NONE;

                    if (_previousEntryState == -1 && _currentEntryState == 1)
                    {
                        orderSide = TradeHubConstants.OrderSide.BUY;
                    }
                    else if (_previousEntryState == 1 && _currentEntryState == -1)
                    {
                        orderSide = TradeHubConstants.OrderSide.SELL;
                    }

                if (Logger.IsDebugEnabled)
                {
                    Logger.Debug("Order Side : " + orderSide, _type.FullName, "EntrySignal");
                }
                return orderSide;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "EntrySignal");
                return TradeHubConstants.OrderSide.NONE;
            }
        }

        /// <summary>
        /// Sends enrty order to the Order Execution Service
        /// </summary>
        /// <param name="orderSide">Order side on which to open position</param>
        /// <param name="symbol">Symbol on which to send order</param>
        private void SendEntryOrder(string orderSide, string symbol)
        {
            try
            {
                if (!orderSide.Equals(TradeHubConstants.OrderSide.NONE))
                {
                    _entryOrderSent = true;

                    ConsoleWriter.WriteLine(ConsoleColor.Green, "Sending" + orderSide + " entry order.");

                    if (Logger.IsInfoEnabled)
                    {
                        Logger.Info("Sending" + orderSide + " entry order.", _type.FullName, "SendEntryOrder");
                    }

                    // Get new unique ID
                    var id = (_orderId++).ToString("X");

                    Security security= new Security{Symbol = symbol};

                    // Create new Limit Order
                    LimitOrder limitOrder = OrderMessage.GenerateLimitOrder(id, security, orderSide, 100, 1.24M, _orderExecutionProvider);

                    // Send Limit Order to OEE
                    SendOrder(limitOrder);
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "SendEntryOrder");
            }
        }

        /// <summary>
        /// Sends exit order to the Order Execution Service
        /// </summary>
        /// <param name="orderSide">Order side on which to open position</param>
        /// <param name="symbol">Symbol on which to send order</param>
        private void SendExitOrder(string orderSide, string symbol)
        {
            try
            {
                if (!orderSide.Equals(TradeHubConstants.OrderSide.NONE))
                {
                    _exitOrderSent = true;

                    ConsoleWriter.WriteLine(ConsoleColor.Green, "Sending" + orderSide + " exit order.");

                    if (Logger.IsInfoEnabled)
                    {
                        Logger.Info("Sending" + orderSide + " exit order.", _type.FullName, "SendExitOrder");
                    }

                    // Get new unique ID
                    var id = (_orderId++).ToString("X");

                    Security security = new Security { Symbol = symbol };

                    // Create new Market Order
                    MarketOrder marketOrder = OrderMessage.GenerateMarketOrder(id, security, orderSide, 100, _orderExecutionProvider);

                    // Send Limit Order to OEE
                    SendOrder(marketOrder);
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "SendExitOrder");
            }
        }

        /// <summary>
        /// Sends market order to the Order Execution Service
        /// </summary>
        /// <param name="orderSide">Order side on which to open position</param>
        /// <param name="symbol">Symbol on which to send order</param>
        private void SendMarketOrder(string orderSide, string symbol)
        {
            try
            {
                if (!orderSide.Equals(TradeHubConstants.OrderSide.NONE))
                {
                    _entryOrderSent = true;

                    ConsoleWriter.WriteLine(ConsoleColor.Green, "Sending" + orderSide + " entry order.");

                    if (Logger.IsInfoEnabled)
                    {
                        Logger.Info("Sending" + orderSide + " entry order.", _type.FullName, "SendMarketOrder");
                    }

                    // Get new unique ID
                    var id = (_orderId++).ToString("X");

                    Security security = new Security { Symbol = symbol };

                    // Create new Limit Order
                    MarketOrder marketOrder = OrderMessage.GenerateMarketOrder(id, security,
                                                                               TradeHubConstants.OrderSide.BUY, 100
                                                                               , _orderExecutionProvider);

                    // Send Limit Order to OEE
                    SendOrder(marketOrder);
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "SendMarketOrder");
            }
        }

        /// <summary>
        /// Called when Logon is received from live market data service 
        /// </summary>
        /// <param name="marketDataProvider">Name of the market data provider</param>
        public override void OnMarketDataServiceLogonArrived(string marketDataProvider)
        {
            try
            {
                ConsoleWriter.WriteLine(ConsoleColor.Green, "Market Data logon received from: " + marketDataProvider);

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Market Data logon received from: " + marketDataProvider, _type.FullName, "OnMarketDataServiceLogonArrived");
                }

                // Send subscription request for tick data
                SubscribeTickData();

                // Send subscription request for live bars
                // SubscribeLiveBars();
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnMarketDataServiceLogonArrived");
            }
        }

        /// <summary>
        /// Called when Logon is received from order data service
        /// </summary>
        /// <param name="orderExecutionProvider">Name of the order execution provider</param>
        public override void OnOrderExecutionServiceLogonArrived(string orderExecutionProvider)
        {
            try
            {
                ConsoleWriter.WriteLine(ConsoleColor.Green, "Order execution logon received from: " + orderExecutionProvider);

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Order execution logon received from: " + orderExecutionProvider, _type.FullName, "OnOrderExecutionServiceLogonArrived");
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnOrderExecutionServiceLogonArrived");
            }
        }

        /// <summary>
        /// Called when New Bar is received
        /// </summary>
        /// <param name="bar">TradeHub Bar containing latest info</param>
        public override void OnBarArrived(Bar bar)
        {
            try
            {
                ConsoleWriter.WriteLine(ConsoleColor.Green, "New bar received : " + bar);

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("New bar received : " + bar, _type.FullName, "OnBarArrived");
                }

                Task.Factory.StartNew(() =>
                    {
                        InitiateTrade(bar);
                    },TaskCreationOptions.PreferFairness);

                //ThreadPool.QueueUserWorkItem(new WaitCallback(InitiateTrade),bar);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnBarArrived");
            }
        }

        /// <summary>
        /// Called when new Tick is received
        /// </summary>
        /// <param name="tick">TradeHub Tick containing lastest info</param>
        public override void OnTickArrived(Tick tick)
        {
            try
            {
                // Call base call for basic operations
                base.OnTickArrived(tick);
                ConsoleWriter.WriteLine(ConsoleColor.Green, "New tick received : " + tick);

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("New tick received : " + tick, _type.FullName, "OnTickArrived");
                }

                //if (_count == 0)
                //{
                //    SendMarketOrder(TradeHubConstants.OrderSide.BUY, _symbol);
                //    _count++;
                //}

                // Dispose();
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnTickArrived");
            }
        }

        /// <summary>
        /// Called when order is accepted by the exchange
        /// </summary>
        /// <param name="order">TradeHub Order</param>
        public override void OnNewArrived(Order order)
        {
            try
            {
                ConsoleWriter.WriteLine(ConsoleColor.Green, "Order accepted: " + order);

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Order accepted: " + order, _type.FullName, "OnNewArrived");
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnNewArrived");
            }
        }

        /// <summary>
        /// Called when Order Execution is received
        /// </summary>
        /// <param name="execution">TradeHub Execution</param>
        public override void OnExecutionArrived(Execution execution)
        {
            try
            {
                ConsoleWriter.WriteLine(ConsoleColor.Green, "Order Execution received : " + execution);

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Order Execution received : " + execution, _type.FullName, "OnExecutionArrived");
                }

                // Resart trading algo on full fill of Exit order
                if (execution.Fill.LeavesQuantity.Equals(0))
                {
                    if (_entryOrderSent)
                    {
                        _entryOrderSent = false;   

                        // Get Order Side for Exit Order
                        var orderSide = execution.Order.OrderSide.Equals(TradeHubConstants.OrderSide.BUY)
                                            ? TradeHubConstants.OrderSide.SELL
                                            : TradeHubConstants.OrderSide.BUY;

                        // Send Exit Order
                        SendExitOrder(orderSide, execution.Order.Security.Symbol);
                    }
                    else if (_exitOrderSent)
                    {
                        _exitOrderSent = false;
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnExecutionArrived");
            }
        }

        /// <summary>
        /// Called when LocateMessage is received from OEE
        /// </summary>
        /// <param name="locateMessage">TradeHub LimitOrder containing details for the LocateMessage</param>
        public override void OnLocateMessageArrived(LimitOrder locateMessage)
        {
            try
            {
                ConsoleWriter.WriteLine(ConsoleColor.Green, "Locate messaeg received : " + locateMessage);

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Locate messaeg received : " + locateMessage, _type.FullName, "OnLocateMessageArrived");
                }

                // Create LocateRespone object
                LocateResponse locateResponse = new LocateResponse(locateMessage.OrderID, _orderExecutionProvider, true);

                // Sends Locate Response to OEP 
                SendLocateResponse(locateResponse);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnLocateMessageArrived");
            }
        }
    }
}
