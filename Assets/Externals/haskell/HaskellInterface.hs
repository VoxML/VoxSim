{-# LANGUAGE ForeignFunctionInterface #-}
module HaskellInterface where

import Foreign.C.String
import Foreign.C.Types

foreign export ccall
  hs_test :: CString -> IO CString

hs_test :: CString -> IO CString
hs_test c_str = do
  str    <- peekCString c_str
  result <- hello str
  c_result <- newCString result
  return c_result

hello :: String -> IO String
hello str = do
-- putStrLn $ "Hello, " ++ str
  return $ ("Hello, " ++ str ++ "I'm Haskell!")
